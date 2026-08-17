using System.Text.Json;
using System.Diagnostics;
using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.EntityFrameworkCore.Auditing;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;

/// <summary>
/// 在 EF Core 儲存生命週期中填入審計值並派發雙軌事件。
/// </summary>
public sealed class AuditAndDomainEventInterceptor : SaveChangesInterceptor {
    private const int MaxDomainEventDispatchLoops = 32;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IAuditUser auditUser;
    private readonly IDomainEventDispatcher domainEventDispatcher;
    private readonly IEventBroadcast eventBroadcast;
    private readonly ICurrentTenant? currentTenant;
    private readonly Dictionary<DbContext, PendingEvents> pendingEventsByContext = new(ReferenceEqualityComparer.Instance);
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// 初始化 <see cref="AuditAndDomainEventInterceptor" /> 類別的新執行個體。
    /// </summary>
    /// <param name="auditUser">審計使用者來源。</param>
    /// <param name="domainEventDispatcher">領域事件派發器。</param>
    /// <param name="eventBroadcast">事件廣播通道。</param>
    /// <param name="timeProvider">時間提供者。</param>
    /// <param name="currentTenant">目前租戶脈絡；未啟用多租戶時為 <see langword="null" />。</param>
    public AuditAndDomainEventInterceptor(
        IAuditUser auditUser,
        IDomainEventDispatcher domainEventDispatcher,
        IEventBroadcast eventBroadcast,
        TimeProvider timeProvider,
        ICurrentTenant? currentTenant = null
    ) {
        ArgumentNullException.ThrowIfNull(auditUser);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        ArgumentNullException.ThrowIfNull(eventBroadcast);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.auditUser = auditUser;
        this.domainEventDispatcher = domainEventDispatcher;
        this.eventBroadcast = eventBroadcast;
        this.timeProvider = timeProvider;
        this.currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    ) {
        if (eventData.Context is null) {
            return result;
        }

        ApplyAuditValues(eventData.Context);
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        CapturePendingEventsAndAddOutboxMessages(eventData.Context);

        return result;
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    ) {
        if (eventData.Context is null) {
            return result;
        }

        PendingEvents pendingEvents = RemovePendingEvents(eventData.Context);
        foreach (IAggregateRoot aggregateRoot in pendingEvents.AggregateRoots) {
            aggregateRoot.ClearDomainEvents();
        }

        foreach (IAggregateRoot aggregateRoot in pendingEvents.AggregateRoots) {
            aggregateRoot.ClearIntegrationEvents();
        }

        foreach (IIntegrationEvent integrationEvent in pendingEvents.IntegrationEvents) {
            await eventBroadcast.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default
    ) {
        if (eventData.Context is not null) {
            PendingEvents pendingEvents = RemovePendingEvents(eventData.Context);
            foreach (OutboxMessage outboxMessage in pendingEvents.OutboxMessages) {
                EntityEntry entry = eventData.Context.Entry(outboxMessage);
                if (entry.State == EntityState.Added) {
                    entry.State = EntityState.Detached;
                }
            }
        }

        return Task.CompletedTask;
    }

    private void ApplyAuditValues(DbContext dbContext) {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid? actorId = AuditScope.ActorId ?? auditUser.UserId;

        foreach (EntityEntry entry in dbContext.ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                ApplyCreatedAuditValues(entry, now, actorId);
                ApplyUpdatedAuditValues(entry, now, actorId);
            } else if (entry.State == EntityState.Modified) {
                ApplyUpdatedAuditValues(entry, now, actorId);
                ApplyDeletedAuditValues(entry, now, actorId);
            }
        }
    }

    private static void ApplyCreatedAuditValues(EntityEntry entry, DateTimeOffset now, Guid? actorId) {
        if (entry.Entity is IHasCreatedTime) {
            entry.Property(nameof(IHasCreatedTime.CreatedTime)).CurrentValue = now;
        }

        if (actorId.HasValue && entry.Entity is IHasCreator) {
            entry.Property(nameof(IHasCreator.CreatorId)).CurrentValue = actorId.Value;
        }
    }

    private static void ApplyUpdatedAuditValues(EntityEntry entry, DateTimeOffset now, Guid? actorId) {
        if (entry.Entity is IHasUpdatedTime) {
            entry.Property(nameof(IHasUpdatedTime.UpdatedTime)).CurrentValue = now;
        }

        if (actorId.HasValue && entry.Entity is IHasUpdater) {
            entry.Property(nameof(IHasUpdater.UpdaterId)).CurrentValue = actorId.Value;
        }
    }

    private static void ApplyDeletedAuditValues(EntityEntry entry, DateTimeOffset now, Guid? actorId) {
        if (entry.Entity is not IHasDeletedTime) {
            return;
        }

        PropertyEntry deletedTimeProperty = entry.Property(nameof(IHasDeletedTime.DeletedTime));
        if (deletedTimeProperty.OriginalValue is DateTimeOffset
            || deletedTimeProperty.CurrentValue is not DateTimeOffset) {
            return;
        }

        if (actorId.HasValue && entry.Entity is IHasDeleter) {
            entry.Property(nameof(IHasDeleter.DeleterId)).CurrentValue = actorId.Value;
        }
    }

    private async Task DispatchDomainEventsAsync(DbContext dbContext, CancellationToken cancellationToken) {
        HashSet<IDomainEvent> dispatchedEvents = new(ReferenceEqualityComparer.Instance);

        for (int loop = 0; loop < MaxDomainEventDispatchLoops; loop++) {
            List<IDomainEvent> domainEvents = GetTrackedAggregateRoots(dbContext)
                .SelectMany(aggregateRoot => aggregateRoot.DomainEvents)
                .Where(domainEvent => !dispatchedEvents.Contains(domainEvent))
                .ToList();

            if (domainEvents.Count == 0) {
                return;
            }

            foreach (IDomainEvent domainEvent in domainEvents) {
                dispatchedEvents.Add(domainEvent);
            }

            await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Domain event dispatch did not converge within {MaxDomainEventDispatchLoops} loops."
        );
    }

    private void CapturePendingEventsAndAddOutboxMessages(DbContext dbContext) {
        IReadOnlyList<IAggregateRoot> aggregateRoots = GetTrackedAggregateRoots(dbContext).ToList();
        IReadOnlyList<IIntegrationEvent> integrationEvents = aggregateRoots
            .SelectMany(aggregateRoot => aggregateRoot.IntegrationEvents)
            .ToList();
        List<OutboxMessage> outboxMessages = [];
        if (dbContext.Model.FindEntityType(typeof(OutboxMessage)) is not null) {
            DateTimeOffset now = timeProvider.GetUtcNow();
            foreach (IIntegrationEvent integrationEvent in integrationEvents) {
                Type eventType = integrationEvent.GetType();
                OutboxMessage message = new() {
                    Id = Guid.CreateVersion7(),
                    TenantId = currentTenant?.TenantId,
                    EventType = eventType.AssemblyQualifiedName
                        ?? throw new InvalidOperationException($"{eventType.FullName} 沒有 assembly-qualified 名稱。"),
                    Payload = JsonSerializer.Serialize(integrationEvent, eventType, SerializerOptions),
                    TraceParent = Activity.Current?.Id,
                    OccurredOn = integrationEvent.OccurredOn,
                    Status = OutboxMessageStatus.Pending,
                    NextRetryOn = now
                };
                outboxMessages.Add(message);
                dbContext.Set<OutboxMessage>().Add(message);
            }
        }

        pendingEventsByContext[dbContext] = new PendingEvents(aggregateRoots, integrationEvents, outboxMessages);
    }

    private PendingEvents RemovePendingEvents(DbContext dbContext) {
        if (pendingEventsByContext.Remove(dbContext, out PendingEvents? pendingEvents)) {
            return pendingEvents;
        }

        List<IAggregateRoot> aggregateRoots = GetTrackedAggregateRoots(dbContext).ToList();
        IReadOnlyList<IIntegrationEvent> integrationEvents = aggregateRoots
            .SelectMany(aggregateRoot => aggregateRoot.IntegrationEvents)
            .ToList();

        return new PendingEvents(aggregateRoots, integrationEvents, []);
    }

    private static IEnumerable<IAggregateRoot> GetTrackedAggregateRoots(DbContext dbContext) {
        return dbContext.ChangeTracker
            .Entries()
            .Select(entry => entry.Entity)
            .OfType<IAggregateRoot>();
    }

    private sealed class PendingEvents {
        public PendingEvents(
            IReadOnlyList<IAggregateRoot> aggregateRoots,
            IReadOnlyList<IIntegrationEvent> integrationEvents,
            IReadOnlyList<OutboxMessage> outboxMessages
        ) {
            AggregateRoots = aggregateRoots;
            IntegrationEvents = integrationEvents;
            OutboxMessages = outboxMessages;
        }

        public IReadOnlyList<IAggregateRoot> AggregateRoots { get; }

        public IReadOnlyList<IIntegrationEvent> IntegrationEvents { get; }

        public IReadOnlyList<OutboxMessage> OutboxMessages { get; }
    }
}
