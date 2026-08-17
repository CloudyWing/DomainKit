using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class QueryServiceKeyTests {
    [Test]
    public async Task GetAsync_EntityWithExternalKey_UsesExternalKeyProjectionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate entity = new() {
            Name = "External-Key-Entity",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(entity);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();
        AuditedAggregateDomainKeyQueryService sut = new(database.DbContext);

        string actual = await sut.GetAsync(item => item.Name, entity.ExternalKey).ConfigureAwait(false);

        Assert.That(actual, Is.EqualTo("External-Key-Entity"));
    }

    [Test]
    public async Task FindAsync_EntityWithoutExternalKey_FallsBackToPrimaryKeyAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        HardDeleteEntity entity = new() {
            Name = "Primary-Key-Entity"
        };
        database.DbContext.HardDeleteEntities.Add(entity);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();
        HardDeleteDomainKeyQueryService sut = new(database.DbContext);

        string? actual = await sut.FindAsync(item => item.Name, entity.Id).ConfigureAwait(false);

        Assert.That(actual, Is.EqualTo("Primary-Key-Entity"));
    }

    [Test]
    public async Task AnyAsync_ExistingExternalKeyVariable_ReturnsTrueAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate entity = new() {
            Name = "Existing-External-Key",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(entity);
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();
        AuditedAggregateDomainKeyQueryService sut = new(database.DbContext);
        Guid key = entity.ExternalKey;

        bool actual = await sut.AnyAsync(key).ConfigureAwait(false);

        Assert.That(actual, Is.True);
    }

    [Test]
    public async Task AnyAsync_UnknownExternalKeyLiteral_ReturnsFalseAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        AuditedAggregateDomainKeyQueryService sut = new(database.DbContext);

        bool actual = await sut.AnyAsync(Guid.CreateVersion7()).ConfigureAwait(false);

        Assert.That(actual, Is.False);
    }

    [Test]
    public async Task AnyAsync_WithPredicate_UsesBasePredicateOverloadAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        database.DbContext.AuditedAggregates.Add(
            new AuditedAggregate {
                Name = "Predicate-Match",
                ExternalKey = Guid.CreateVersion7()
            }
        );
        await unitOfWork.SaveChangesAsync().ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();
        AuditedAggregateDomainKeyQueryService sut = new(database.DbContext);

        bool actual = await sut.AnyAsync(entity => entity.Name == "Predicate-Match").ConfigureAwait(false);

        Assert.That(actual, Is.True);
    }

    private sealed class AuditedAggregateDomainKeyQueryService : QueryService<TestDbContext, AuditedAggregate, Guid> {
        public AuditedAggregateDomainKeyQueryService(TestDbContext dbContext)
            : base(dbContext) {
        }
    }

    private sealed class HardDeleteDomainKeyQueryService : QueryService<TestDbContext, HardDeleteEntity, Guid> {
        public HardDeleteDomainKeyQueryService(TestDbContext dbContext)
            : base(dbContext) {
        }
    }
}
