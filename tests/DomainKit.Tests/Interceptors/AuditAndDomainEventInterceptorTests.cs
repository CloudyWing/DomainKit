using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using CloudyWing.DomainKit.Tenancy;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Interceptors;

[TestFixture]
public sealed class AuditAndDomainEventInterceptorTests {
    [Test]
    public async Task SaveChangesAsync_IHasRowVersion_StampsRowVersionOnAddAndUpdateAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate aggregate = new() {
            Name = "RowVersion",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(aggregate);

        Assert.That(aggregate.RowVersion, Is.Empty);

        await unitOfWork.SaveChangesAsync();
        byte[] originalRowVersion = aggregate.RowVersion.ToArray();

        aggregate.Name = "RowVersionUpdated";
        await unitOfWork.SaveChangesAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(originalRowVersion, Is.Not.Empty);
            Assert.That(aggregate.RowVersion, Is.Not.Empty);
            Assert.That(aggregate.RowVersion.SequenceEqual(originalRowVersion), Is.False);
        }
    }

    [Test]
    public async Task SaveChangesAsync_AddedAuditedEntity_FillsCreatedAndUpdatedAuditValuesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Guid actorId = Guid.CreateVersion7();
        AuditedAggregate aggregate = new() {
            Name = "Audited",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync(actorId);

        using (Assert.EnterMultipleScope()) {
            Assert.That(aggregate.CreatedTime, Is.Not.EqualTo(default(DateTimeOffset)));
            Assert.That(aggregate.UpdatedTime, Is.Not.EqualTo(default(DateTimeOffset)));
            Assert.That(aggregate.CreatorId, Is.EqualTo(actorId));
            Assert.That(aggregate.UpdaterId, Is.EqualTo(actorId));
        }
    }

    [Test]
    public async Task SaveChangesAsync_AddedCreationAuditedEntity_FillsCreatedAuditValuesOnlyAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Guid actorId = Guid.CreateVersion7();
        CreationAuditedEntity entity = new() {
            Name = "Creation"
        };
        database.DbContext.CreationAuditedEntities.Add(entity);

        await unitOfWork.SaveChangesAsync(actorId);

        using (Assert.EnterMultipleScope()) {
            Assert.That(entity.CreatedTime, Is.Not.EqualTo(default(DateTimeOffset)));
            Assert.That(entity.CreatorId, Is.EqualTo(actorId));
            Assert.That(database.DbContext.Entry(entity).Metadata.FindProperty("UpdatedTime"), Is.Null);
            Assert.That(database.DbContext.Entry(entity).Metadata.FindProperty("DeletedTime"), Is.Null);
        }
    }

    [Test]
    public async Task DeleteAsync_SoftDelete_FillsDeletedAuditValuesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, AuditedAggregate, Guid> repository = new(database.DbContext, unitOfWork);
        Guid actorId = Guid.CreateVersion7();
        Guid externalKey = Guid.CreateVersion7();
        AuditedAggregate aggregate = new() {
            Name = "Soft",
            ExternalKey = externalKey
        };
        database.DbContext.AuditedAggregates.Add(aggregate);
        await unitOfWork.SaveChangesAsync(actorId);

        await repository.DeleteAsync(externalKey, autoCommit: true, actorId: actorId);

        AuditedAggregate stored = await database.DbContext.AuditedAggregates
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.ExternalKey == externalKey);
        using (Assert.EnterMultipleScope()) {
            Assert.That(stored.DeletedTime, Is.Not.Null);
            Assert.That(stored.DeleterId, Is.EqualTo(actorId));
        }
    }

    [Test]
    public async Task SaveChangesAsync_DomainEvent_DispatchesBeforeCommitAndClearsEventAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        SideEffectEntity sideEffect = new() {
            Note = "pending"
        };
        AuditedAggregate aggregate = new() {
            Name = "Aggregate",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseDomainEvent(sideEffect.Id);
        database.DbContext.SideEffects.Add(sideEffect);
        database.DbContext.AuditedAggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
        SideEffectEntity storedSideEffect = await database.DbContext.SideEffects.SingleAsync(entity => entity.Id == sideEffect.Id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(recorder.DomainEvents.OfType<TestDomainEvent>().Count(), Is.EqualTo(1));
            Assert.That(storedSideEffect.Note, Is.EqualTo("handled"));
            Assert.That(aggregate.DomainEvents, Is.Empty);
        }
    }

    [Test]
    public async Task SaveChangesAsync_DomainHandlerRaisesFollowUpEvent_DispatchesUntilConvergedAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        SideEffectEntity sideEffect = new() {
            Note = "pending"
        };
        AuditedAggregate aggregate = new() {
            Name = "Aggregate",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseDomainEvent(sideEffect.Id, raiseFollowUp: true);
        database.DbContext.SideEffects.Add(sideEffect);
        database.DbContext.AuditedAggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
        SideEffectEntity storedSideEffect = await database.DbContext.SideEffects.SingleAsync(entity => entity.Id == sideEffect.Id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(recorder.DomainEvents.OfType<TestDomainEvent>().Count(), Is.EqualTo(1));
            Assert.That(recorder.DomainEvents.OfType<FollowUpDomainEvent>().Count(), Is.EqualTo(1));
            Assert.That(storedSideEffect.Note, Is.EqualTo("follow-up"));
            Assert.That(aggregate.DomainEvents, Is.Empty);
        }
    }

    [Test]
    public async Task SaveChangesAsync_IntegrationEvent_DispatchesAfterCommitAndClearsEventAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        AuditedAggregate aggregate = new() {
            Name = "Integration",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseIntegrationEvent();
        database.DbContext.AuditedAggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync();
        await unitOfWork.SaveChangesAsync();
        OutboxMessage pending = await database.DbContext.Set<OutboxMessage>().SingleAsync();
        OutboxDispatcherHostedService<TestDbContext> dispatcher = database.Services
            .GetServices<IHostedService>()
            .OfType<OutboxDispatcherHostedService<TestDbContext>>()
            .Single();
        await dispatcher.DispatchBatchAsync(default);

        using (Assert.EnterMultipleScope()) {
            Assert.That(pending.Status, Is.EqualTo(OutboxMessageStatus.Pending));
            Assert.That(recorder.IntegrationEvents.OfType<TestIntegrationEvent>().Count(), Is.EqualTo(1));
            Assert.That(aggregate.IntegrationEvents, Is.Empty);
        }
    }

    [Test]
    public async Task DispatchBatchAsync_TenantIntegrationEvent_RestoresTenantForHandlerAsync() {
        AmbientCurrentTenant currentTenant = new();
        await using TestDatabase database = await TestDatabase.CreateAsync(
            registerHandlers: true,
            configureServices: services => services.AddSingleton<ICurrentTenant>(currentTenant)
        );
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        Guid tenantId = Guid.CreateVersion7();
        AuditedAggregate aggregate = new() {
            Name = "TenantIntegration",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseIntegrationEvent();
        database.DbContext.AuditedAggregates.Add(aggregate);

        using (currentTenant.Change(tenantId)) {
            await unitOfWork.SaveChangesAsync();
        }

        OutboxMessage message = await database.DbContext.Set<OutboxMessage>().SingleAsync();
        OutboxDispatcherHostedService<TestDbContext> dispatcher = database.Services
            .GetServices<IHostedService>()
            .OfType<OutboxDispatcherHostedService<TestDbContext>>()
            .Single();
        await dispatcher.DispatchBatchAsync(default);

        using (Assert.EnterMultipleScope()) {
            Assert.That(message.TenantId, Is.EqualTo(tenantId));
            Assert.That(recorder.IntegrationEventTenantIds, Is.EqualTo([tenantId]));
            Assert.That(currentTenant.TenantId, Is.Null);
        }
    }

    [Test]
    public async Task SaveChangesAsync_IntegrationHandlerPerformsNestedSave_DispatchesOnlyOnceAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        AuditedAggregate aggregate = new() {
            Name = "ReentrantIntegration",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseReentrantIntegrationEvent();
        database.DbContext.AuditedAggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync();
        OutboxDispatcherHostedService<TestDbContext> dispatcher = database.Services
            .GetServices<IHostedService>()
            .OfType<OutboxDispatcherHostedService<TestDbContext>>()
            .Single();
        await dispatcher.DispatchBatchAsync(default);
        database.DbContext.ChangeTracker.Clear();

        using (Assert.EnterMultipleScope()) {
            Assert.That(recorder.IntegrationEvents.OfType<ReentrantIntegrationEvent>().Count(), Is.EqualTo(1));
            Assert.That(database.DbContext.HardDeleteEntities.Count(), Is.EqualTo(1));
            Assert.That(aggregate.IntegrationEvents, Is.Empty);
        }
    }

    [Test]
    public async Task SaveChangesAsync_DatabaseWriteFails_DoesNotDispatchIntegrationEventAndKeepsEventAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(registerHandlers: true);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        EventRecorder recorder = database.Services.GetRequiredService<EventRecorder>();
        AuditedAggregate aggregate = new() {
            Name = null!,
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseIntegrationEvent();
        database.DbContext.AuditedAggregates.Add(aggregate);

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await unitOfWork.SaveChangesAsync()
        );

        using (Assert.EnterMultipleScope()) {
            Assert.That(recorder.IntegrationEvents, Is.Empty);
            Assert.That(aggregate.IntegrationEvents, Has.Count.EqualTo(1));
            Assert.That(await database.DbContext.Set<OutboxMessage>().CountAsync(), Is.Zero);
        }
    }
}
