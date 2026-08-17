using System.Security.Cryptography;
using System.Text;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Tenancy;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Events;

[TestFixture]
public sealed class OutboxInfrastructureTests {
    [Test]
    public async Task PublishEnvelopeAsync_TenantContext_CapturesTenantAndOccurredTimeAsync() {
        InProcessEventBroadcast broadcast = new();
        AmbientCurrentTenant currentTenant = new();
        Guid tenantId = Guid.CreateVersion7();
        DateTimeOffset occurredOn = new(2026, 7, 18, 8, 30, 0, TimeSpan.Zero);
        BroadcastEnvelope<TestIntegrationEvent>? received = null;
        using IDisposable subscription = broadcast.Subscribe<BroadcastEnvelope<TestIntegrationEvent>>(
            (envelope, _) => {
                received = envelope;
                return Task.CompletedTask;
            }
        );
        TestIntegrationEvent payload = new(Guid.CreateVersion7());

        using (currentTenant.Change(tenantId)) {
            await broadcast.PublishEnvelopeAsync(
                payload,
                currentTenant,
                new FixedTimeProvider(occurredOn)
            );
        }

        using (Assert.EnterMultipleScope()) {
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.TenantId, Is.EqualTo(tenantId));
            Assert.That(received.Payload, Is.SameAs(payload));
            Assert.That(received.OccurredOn, Is.EqualTo(occurredOn));
        }
    }

    [Test]
    public async Task SubscribeEnvelope_EnvelopeTenant_RestoresTenantForHandlerAndCallerAsync() {
        InProcessEventBroadcast broadcast = new();
        AmbientCurrentTenant currentTenant = new();
        Guid callerTenantId = Guid.CreateVersion7();
        Guid envelopeTenantId = Guid.CreateVersion7();
        Guid? handlerTenantId = null;
        TestIntegrationEvent payload = new(Guid.CreateVersion7());
        using IDisposable subscription = broadcast.SubscribeEnvelope<TestIntegrationEvent>(
            currentTenant,
            (received, _) => {
                Assert.That(received, Is.SameAs(payload));
                handlerTenantId = currentTenant.TenantId;
                return Task.CompletedTask;
            }
        );

        using (currentTenant.Change(callerTenantId)) {
            await broadcast.PublishAsync(
                new BroadcastEnvelope<TestIntegrationEvent>(envelopeTenantId, payload, DateTimeOffset.UtcNow)
            );

            using (Assert.EnterMultipleScope()) {
                Assert.That(handlerTenantId, Is.EqualTo(envelopeTenantId));
                Assert.That(currentTenant.TenantId, Is.EqualTo(callerTenantId));
            }
        }
    }

    [Test]
    public async Task Broadcast_MultipleSubscribersAndFailure_NotifiesRemainingSubscribersAsync() {
        InProcessEventBroadcast broadcast = new();
        int received = 0;
        using IDisposable failing = broadcast.Subscribe<TestIntegrationEvent>(
            (_, _) => throw new InvalidOperationException("Simulated subscriber failure.")
        );
        using IDisposable first = broadcast.Subscribe<TestIntegrationEvent>((_, _) => {
            received++;
            return Task.CompletedTask;
        });
        using IDisposable second = broadcast.Subscribe<TestIntegrationEvent>((_, _) => {
            received++;
            return Task.CompletedTask;
        });

        await broadcast.PublishAsync(new TestIntegrationEvent(Guid.CreateVersion7()));

        Assert.That(received, Is.EqualTo(2));
    }

    [Test]
    public async Task Dispatcher_HandlerFailsThenSucceeds_RetriesAndMarksProcessedAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        recorder.IntegrationFailuresRemaining = 1;
        await AddIntegrationEventAsync(database);
        OutboxDispatcherHostedService<TestDbContext> dispatcher = GetDispatcher(database);

        await dispatcher.DispatchBatchAsync(default);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage failed = await database.DbContext.Set<OutboxMessage>().SingleAsync();
        failed.NextRetryOn = DateTimeOffset.MinValue;
        await database.DbContext.SaveChangesAsync();
        await dispatcher.DispatchBatchAsync(default);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage processed = await database.DbContext.Set<OutboxMessage>().SingleAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(recorder.IntegrationHandleAttempts, Is.EqualTo(2));
            Assert.That(recorder.IntegrationEvents, Has.Count.EqualTo(1));
            Assert.That(processed.Status, Is.EqualTo(OutboxMessageStatus.Processed));
            Assert.That(processed.RetryCount, Is.EqualTo(1));
            Assert.That(await database.DbContext.Set<ProcessedEvent>().CountAsync(), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Dispatcher_RetryLimitExceeded_MarksDeadLetteredAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(
            registerHandlers: true,
            configureDomainKit: options => options.Outbox.MaxRetryCount = 1
        );
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        recorder.IntegrationFailuresRemaining = 2;
        await AddIntegrationEventAsync(database);
        OutboxDispatcherHostedService<TestDbContext> dispatcher = GetDispatcher(database);

        await dispatcher.DispatchBatchAsync(default);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage failed = await database.DbContext.Set<OutboxMessage>().SingleAsync();
        failed.NextRetryOn = DateTimeOffset.MinValue;
        await database.DbContext.SaveChangesAsync();
        await dispatcher.DispatchBatchAsync(default);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage deadLettered = await database.DbContext.Set<OutboxMessage>().SingleAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(recorder.IntegrationHandleAttempts, Is.EqualTo(2));
            Assert.That(deadLettered.Status, Is.EqualTo(OutboxMessageStatus.DeadLettered));
            Assert.That(deadLettered.RetryCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task Dispatcher_ReplayedProcessedEvent_DoesNotInvokeHandlerTwiceAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        await AddIntegrationEventAsync(database);
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        OutboxDispatcherHostedService<TestDbContext> dispatcher = GetDispatcher(database);

        await dispatcher.DispatchBatchAsync(default);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage message = await database.DbContext.Set<OutboxMessage>().SingleAsync();
        message.Status = OutboxMessageStatus.Pending;
        message.ProcessedOn = null;
        message.NextRetryOn = DateTimeOffset.MinValue;
        await database.DbContext.SaveChangesAsync();
        await dispatcher.DispatchBatchAsync(default);

        Assert.That(recorder.IntegrationEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Dispatcher_ProcessedEvent_StoresFixedLengthHandlerKeyAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        await AddIntegrationEventAsync(database);
        OutboxDispatcherHostedService<TestDbContext> dispatcher = GetDispatcher(database);

        await dispatcher.DispatchBatchAsync(default);
        ProcessedEvent processed = await database.DbContext.Set<ProcessedEvent>().SingleAsync();
        string legacyName = typeof(TestIntegrationEventHandler).AssemblyQualifiedName
            ?? throw new InvalidOperationException("測試處理器缺少組件限定名稱。");
        string expectedKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(legacyName)));

        using (Assert.EnterMultipleScope()) {
            Assert.That(processed.HandlerName, Is.EqualTo(expectedKey));
            Assert.That(processed.HandlerName, Has.Length.EqualTo(64));
            Assert.That(processed.HandlerName, Does.Not.Contain(nameof(TestIntegrationEventHandler)));
        }
    }

    [Test]
    public async Task SaveChangesAsync_TransactionRollsBack_DoesNotPersistOutboxMessageAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();

        Assert.ThrowsAsync<InvalidOperationException>(async () => await unitOfWork.UseTransactionAsync(
            async cancellationToken => {
                await AddIntegrationEventAsync(database);
                throw new InvalidOperationException("Rollback transaction.");
            }
        ));
        database.DbContext.ChangeTracker.Clear();

        Assert.That(await database.DbContext.Set<OutboxMessage>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Dispatchers_ClaimSamePendingMessage_HandlerRunsOnceAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        await AddIntegrationEventAsync(database);
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        OutboxDispatcherHostedService<TestDbContext> first = GetDispatcher(database);
        OutboxDispatcherHostedService<TestDbContext> second = ActivatorUtilities.CreateInstance<
            OutboxDispatcherHostedService<TestDbContext>>(database.Services);

        await Task.WhenAll(
            first.DispatchBatchAsync(default),
            second.DispatchBatchAsync(default)
        );

        Assert.That(recorder.IntegrationEvents, Has.Count.EqualTo(1));
    }

    private static async Task AddIntegrationEventAsync(TestDatabase database) {
        AuditedAggregate aggregate = new() {
            Name = "Outbox",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseIntegrationEvent();
        database.DbContext.AuditedAggregates.Add(aggregate);
        await database.Services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    private static OutboxDispatcherHostedService<TestDbContext> GetDispatcher(TestDatabase database) {
        return database.Services.GetServices<IHostedService>()
            .OfType<OutboxDispatcherHostedService<TestDbContext>>()
            .Single();
    }

    private sealed class FixedTimeProvider : TimeProvider {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow) {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() {
            return utcNow;
        }
    }
}
