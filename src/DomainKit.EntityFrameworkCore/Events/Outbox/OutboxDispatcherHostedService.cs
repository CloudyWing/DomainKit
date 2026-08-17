using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 認領並派發持久化整合事件。
/// </summary>
/// <typeparam name="TDbContext">DbContext 型別。</typeparam>
public sealed class OutboxDispatcherHostedService<TDbContext> : BackgroundService
    where TDbContext : DbContext {
    private static readonly Action<ILogger, Exception?> DispatcherFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, nameof(DispatcherFailed)),
        "Outbox dispatcher 執行失敗，下一輪將重試。"
    );
    private static readonly Action<ILogger, Guid, Exception?> MessageDispatchFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(2, nameof(MessageDispatchFailed)),
        "Outbox 訊息 {OutboxMessageId} 派發失敗。"
    );
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly string instanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    private readonly ILogger<OutboxDispatcherHostedService<TDbContext>> logger;
    private readonly OutboxOptions options;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// 初始化 <see cref="OutboxDispatcherHostedService{TDbContext}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="scopeFactory">服務 scope factory。</param>
    /// <param name="options">Outbox 選項。</param>
    /// <param name="timeProvider">時間提供者。</param>
    /// <param name="logger">Logger。</param>
    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxDispatcherHostedService<TDbContext>> logger
    ) {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        this.scopeFactory = scopeFactory;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
        ValidateOptions(this.options);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                int dispatched = await DispatchBatchAsync(stoppingToken).ConfigureAwait(false);
                if (dispatched == 0) {
                    await Task.Delay(options.PollingInterval, timeProvider, stoppingToken).ConfigureAwait(false);
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                DispatcherFailed(logger, ex);
                await Task.Delay(options.PollingInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    internal async Task<int> DispatchBatchAsync(CancellationToken cancellationToken) {
        DateTimeOffset now = timeProvider.GetUtcNow();
        await CleanupProcessedAsync(now, cancellationToken).ConfigureAwait(false);

        await using AsyncServiceScope claimScope = scopeFactory.CreateAsyncScope();
        IDbContextFactory<TDbContext> claimFactory = claimScope.ServiceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>();
        await using TDbContext claimContext = await claimFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        int pendingCount = await claimContext.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(
                message => message.Status == OutboxMessageStatus.Pending,
                cancellationToken
            )
            .ConfigureAwait(false);
        DomainKitDiagnostics.SetPendingCount(pendingCount);
        List<Guid> candidateIds = await claimContext.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .Where(message => message.Status == OutboxMessageStatus.Pending
                && message.NextRetryOn <= now
                && (!message.LockedUntil.HasValue || message.LockedUntil.Value < now))
            .OrderBy(message => message.Id)
            .Take(options.BatchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int claimed = 0;
        foreach (Guid id in candidateIds) {
            int affected = await claimContext.Set<OutboxMessage>()
                .IgnoreQueryFilters()
                .Where(message => message.Id == id
                    && message.Status == OutboxMessageStatus.Pending
                    && (!message.LockedUntil.HasValue || message.LockedUntil.Value < now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, OutboxMessageStatus.Processing)
                        .SetProperty(message => message.LockedBy, instanceId)
                        .SetProperty(message => message.LockedUntil, now.Add(options.LockDuration)),
                    cancellationToken
                )
                .ConfigureAwait(false);
            if (affected != 1) {
                continue;
            }

            claimed++;
            await DispatchOneAsync(id, cancellationToken).ConfigureAwait(false);
        }

        return claimed;
    }

    private async Task DispatchOneAsync(Guid id, CancellationToken cancellationToken) {
        try {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            OutboxMessage message = await dbContext.Set<OutboxMessage>()
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == id, cancellationToken)
                .ConfigureAwait(false);
            ICurrentTenant? currentTenant = scope.ServiceProvider.GetService<ICurrentTenant>();
            if (message.TenantId.HasValue && currentTenant is null) {
                throw new InvalidOperationException("Outbox 訊息具有租戶識別碼，但缺少 ICurrentTenant 服務。");
            }

            using IDisposable? tenantScope = message.TenantId.HasValue
                ? currentTenant!.Change(message.TenantId)
                : null;
            ActivityLink[] links = CreateTraceLinks(message.TraceParent);
            using Activity? activity = DomainKitDiagnostics.ActivitySource.StartActivity(
                "Outbox dispatch",
                ActivityKind.Consumer,
                default(ActivityContext),
                tags: null,
                links: links
            );
            activity?.SetTag("outbox.message.id", message.Id);
            if (message.TenantId.HasValue) {
                activity?.SetTag("tenant.id", message.TenantId.Value);
            }
            Type eventType = Type.GetType(message.EventType, throwOnError: true)!;
            object eventData = JsonSerializer.Deserialize(message.Payload, eventType, SerializerOptions)
                ?? throw new InvalidOperationException("Outbox 事件內容無法反序列化。");
            if (eventData is not IIntegrationEvent) {
                throw new InvalidOperationException($"{eventType.FullName} 未實作 IIntegrationEvent。");
            }

            IntegrationEventContext context = scope.ServiceProvider.GetRequiredService<IntegrationEventContext>();
            context.SetEventId(message.Id);
            Type handlerServiceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerServiceType);
            foreach (object handler in handlers.OfType<object>()) {
                string handlerName = CreateHandlerKey(handler.GetType());
                string legacyHandlerName = CreateLegacyHandlerName(handler.GetType());
                bool alreadyProcessed = await dbContext.Set<ProcessedEvent>()
                    .AnyAsync(
                        processed => (processed.HandlerName == handlerName
                            || processed.HandlerName == legacyHandlerName)
                            && processed.EventId == message.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                if (alreadyProcessed) {
                    continue;
                }

                dbContext.Set<ProcessedEvent>().Add(new ProcessedEvent {
                    HandlerName = handlerName,
                    EventId = message.Id,
                    ProcessedOn = timeProvider.GetUtcNow()
                });
                await EventHandlerInvoker.InvokeAsync(
                    handler,
                    eventData,
                    typeof(IIntegrationEventHandler<>),
                    cancellationToken
                ).ConfigureAwait(false);
            }

            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedOn = timeProvider.GetUtcNow();
            message.LockedBy = null;
            message.LockedUntil = null;
            message.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            DomainKitDiagnostics.OutboxDispatchDuration.Record(
                (timeProvider.GetUtcNow() - message.OccurredOn).TotalMilliseconds
            );
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            await MarkFailedAsync(id, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task MarkFailedAsync(Guid id, Exception exception, CancellationToken cancellationToken) {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IDbContextFactory<TDbContext> dbContextFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>();
        await using TDbContext dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        OutboxMessage? message = await dbContext.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (message is null) {
            return;
        }

        message.RetryCount++;
        message.LockedBy = null;
        message.LockedUntil = null;
        message.LastError = exception.ToString().Length <= 2000
            ? exception.ToString()
            : exception.ToString()[..2000];
        if (message.RetryCount > options.MaxRetryCount) {
            message.Status = OutboxMessageStatus.DeadLettered;
            DomainKitDiagnostics.OutboxDeadLetter.Add(1);
        } else {
            double multiplier = Math.Pow(2, message.RetryCount);
            TimeSpan delay = TimeSpan.FromTicks(Math.Min(
                options.MaxRetryDelay.Ticks,
                (long)(options.BaseRetryDelay.Ticks * multiplier)
            ));
            message.Status = OutboxMessageStatus.Pending;
            message.NextRetryOn = timeProvider.GetUtcNow().Add(delay);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        MessageDispatchFailed(logger, id, exception);
    }

    private async Task CleanupProcessedAsync(DateTimeOffset now, CancellationToken cancellationToken) {
        DateTimeOffset cutoff = now.Subtract(options.RetentionPeriod);
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IDbContextFactory<TDbContext> dbContextFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>();
        await using TDbContext dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        List<ProcessedEvent> processedEvents = await dbContext.Set<ProcessedEvent>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        List<ProcessedEvent> expiredProcessedEvents = processedEvents
            .Where(processed => processed.ProcessedOn < cutoff)
            .Take(options.BatchSize)
            .ToList();
        List<OutboxMessage> processedOutboxMessages = await dbContext.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .Where(message => message.Status == OutboxMessageStatus.Processed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        List<OutboxMessage> expiredOutboxMessages = processedOutboxMessages
            .Where(message => message.ProcessedOn.HasValue && message.ProcessedOn.Value < cutoff)
            .Take(options.BatchSize)
            .ToList();
        if (expiredProcessedEvents.Count == 0 && expiredOutboxMessages.Count == 0) {
            return;
        }

        dbContext.Set<ProcessedEvent>().RemoveRange(expiredProcessedEvents);
        dbContext.Set<OutboxMessage>().RemoveRange(expiredOutboxMessages);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateOptions(OutboxOptions options) {
        if (options.BatchSize <= 0
            || options.PollingInterval <= TimeSpan.Zero
            || options.LockDuration <= TimeSpan.Zero
            || options.BaseRetryDelay <= TimeSpan.Zero
            || options.MaxRetryDelay < options.BaseRetryDelay
            || options.MaxRetryCount < 0
            || options.RetentionPeriod <= TimeSpan.Zero) {
            throw new InvalidOperationException("Outbox 選項包含無效值。");
        }
    }

    private static string CreateHandlerKey(Type handlerType) {
        string identity = CreateLegacyHandlerName(handlerType);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static string CreateLegacyHandlerName(Type handlerType) {
        return handlerType.AssemblyQualifiedName
            ?? handlerType.FullName
            ?? handlerType.Name;
    }

    private static ActivityLink[] CreateTraceLinks(string? traceParent) {
        return ActivityContext.TryParse(traceParent, null, isRemote: true, out ActivityContext context)
            ? [new ActivityLink(context)]
            : [];
    }
}
