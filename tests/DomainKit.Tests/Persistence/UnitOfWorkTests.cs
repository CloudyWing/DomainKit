using System.Reflection;
using CloudyWing.DomainKit.Exceptions;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class UnitOfWorkTests {
    [Test]
    public async Task SaveChangesAsync_PendingEntity_PersistsChangesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();
        database.DbContext.HardDeleteEntities.Add(
            new HardDeleteEntity {
                Name = "UnitOfWork"
            }
        );

        int affectedRows = await sut.SaveChangesAsync();
        int count = await database.DbContext.HardDeleteEntities.AsNoTracking().CountAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(affectedRows, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UseTransactionAsync_NoCurrentTransaction_CommitsTransactionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();

        await sut.UseTransactionAsync(
            async cancellationToken => {
                database.DbContext.HardDeleteEntities.Add(
                    new HardDeleteEntity {
                        Name = "Committed"
                    }
                );
                await sut.SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        );

        int count = await database.DbContext.HardDeleteEntities.AsNoTracking().CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UseTransactionAsync_CurrentTransaction_ReusesExistingTransactionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();
        await using IDbContextTransaction outerTransaction = await database.DbContext.Database.BeginTransactionAsync();

        IDbContextTransaction actual = await sut.UseTransactionAsync(
            (transaction, _) => Task.FromResult(transaction)
        );

        Assert.That(actual, Is.SameAs(outerTransaction));
    }

    [Test]
    public async Task SaveChangesAsync_ConcurrencyConflict_ThrowsTranslatedExceptionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork firstUnitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        IServiceScopeFactory scopeFactory = database.Services.GetRequiredService<IServiceScopeFactory>();
        Guid externalKey = Guid.CreateVersion7();
        database.DbContext.AuditedAggregates.Add(
            new AuditedAggregate {
                Name = "Original",
                ExternalKey = externalKey
            }
        );
        await firstUnitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        AuditedAggregate first = await database.DbContext.AuditedAggregates
            .SingleAsync(entity => entity.ExternalKey == externalKey);
        await using AsyncServiceScope secondScope = scopeFactory.CreateAsyncScope();
        TestDbContext secondDbContext = secondScope.ServiceProvider.GetRequiredService<TestDbContext>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        AuditedAggregate second = await secondDbContext.AuditedAggregates
            .SingleAsync(entity => entity.ExternalKey == externalKey);

        first.Name = "First";
        await firstUnitOfWork.SaveChangesAsync();
        second.Name = "Second";

        ConcurrencyConflictException? exception = Assert.ThrowsAsync<ConcurrencyConflictException>(
            async () => await secondUnitOfWork.SaveChangesAsync()
        );

        Assert.That(exception?.InnerException, Is.TypeOf<DbUpdateConcurrencyException>());
    }

    [Test]
    public async Task DiscardChanges_ModifiedAggregate_RevertsTrackedStateAndClearsEventsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate aggregate = new() {
            Name = "Original",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(aggregate);
        await sut.SaveChangesAsync();

        aggregate.Name = "Changed";
        aggregate.RaiseIntegrationEvent();
        aggregate.RaiseDomainEvent(Guid.CreateVersion7());

        sut.DiscardChanges();

        AuditedAggregate stored = await database.DbContext.AuditedAggregates
            .SingleAsync(entity => entity.Id == aggregate.Id);
        using (Assert.EnterMultipleScope()) {
            Assert.That(stored.Name, Is.EqualTo("Original"));
            Assert.That(aggregate.DomainEvents, Is.Empty);
            Assert.That(aggregate.IntegrationEvents, Is.Empty);
            Assert.That(database.DbContext.ChangeTracker.Entries(), Is.Not.Empty);
        }
    }

    [Test]
    public void IUnitOfWork_PublicSurface_ExposesEfTransactionAsync() {
        MethodInfo[] methods = typeof(IUnitOfWork).GetMethods();

        Assert.That(
            methods.Any(method => method is { Name: nameof(IUnitOfWork.UseTransactionAsync), IsGenericMethod: true }),
            Is.True
        );
        Assert.That(methods.Any(method => method.Name == nameof(IUnitOfWork.DiscardChanges)), Is.True);
    }
}
