using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Exceptions;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class RepositoryTests {
    [Test]
    public async Task CreateAsync_AutoCommitFalse_DoesNotPersistUntilUnitOfWorkSavesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, AuditedAggregate, Guid> sut = new(database.DbContext, unitOfWork);
        AuditedAggregate entity = new() {
            Name = "Draft",
            ExternalKey = Guid.CreateVersion7()
        };

        await sut.CreateAsync(entity);
        int countBeforeCommit = await database.DbContext.AuditedAggregates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync();
        await unitOfWork.SaveChangesAsync();
        int countAfterCommit = await database.DbContext.AuditedAggregates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(countBeforeCommit, Is.EqualTo(0));
            Assert.That(countAfterCommit, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateAsync_AutoCommitTrue_PersistsImmediatelyAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, AuditedAggregate, Guid> sut = new(database.DbContext, unitOfWork);
        AuditedAggregate entity = new() {
            Name = "Published",
            ExternalKey = Guid.CreateVersion7()
        };

        await sut.CreateAsync(entity, autoCommit: true);

        int count = await database.DbContext.AuditedAggregates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAsync_ExternalKeyEntity_UsesExternalKeyInsteadOfPrimaryKeyAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, ExternalKeyEntity, string> sut = new(database.DbContext, unitOfWork);
        ExternalKeyEntity entity = new() {
            ExternalKey = "external-001",
            Name = "External"
        };
        database.DbContext.ExternalKeyEntities.Add(entity);
        await unitOfWork.SaveChangesAsync();

        ExternalKeyEntity actual = await sut.GetAsync("external-001");

        Assert.That(actual.Id, Is.EqualTo(entity.Id));
    }

    [Test]
    public async Task DeleteAsync_SoftDeletableEntity_MarksDeletedAndKeepsRowAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, AuditedAggregate, Guid> sut = new(database.DbContext, unitOfWork);
        Guid externalKey = Guid.CreateVersion7();
        AuditedAggregate entity = new() {
            Name = "Soft",
            ExternalKey = externalKey
        };
        database.DbContext.AuditedAggregates.Add(entity);
        await unitOfWork.SaveChangesAsync();

        await sut.DeleteAsync(externalKey, autoCommit: true);

        int visibleCount = await database.DbContext.AuditedAggregates.AsNoTracking().CountAsync();
        AuditedAggregate stored = await database.DbContext.AuditedAggregates
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.ExternalKey == externalKey);
        using (Assert.EnterMultipleScope()) {
            Assert.That(visibleCount, Is.EqualTo(0));
            Assert.That(stored.DeletedTime, Is.Not.Null);
        }
    }

    [Test]
    public async Task DeleteAsync_NonSoftDeletableEntity_RemovesRowAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, HardDeleteEntity, Guid> sut = new(database.DbContext, unitOfWork);
        HardDeleteEntity entity = new() {
            Name = "Hard"
        };
        database.DbContext.HardDeleteEntities.Add(entity);
        await unitOfWork.SaveChangesAsync();

        await sut.DeleteAsync(entity.Id, autoCommit: true);

        int count = await database.DbContext.HardDeleteEntities.AsNoTracking().CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAsync_MissingEntity_ThrowsEntityNotFoundExceptionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        Repository<TestDbContext, HardDeleteEntity, Guid> sut = new(database.DbContext, unitOfWork);

        Assert.ThrowsAsync<EntityNotFoundException>(
            async () => await sut.GetAsync(Guid.CreateVersion7())
        );
    }
}
