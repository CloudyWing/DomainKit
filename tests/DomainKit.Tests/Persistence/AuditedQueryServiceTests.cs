using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class AuditedQueryServiceTests {
    [Test]
    public async Task GetListAsync_CreatorAndUpdaterExist_ReturnsJoinedUserNamesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        TestUserEntity creator = new() {
            DisplayName = "Creator-A"
        };
        TestUserEntity updater = new() {
            DisplayName = "Updater-A"
        };
        database.DbContext.TestUsers.AddRange(creator, updater);
        await database.DbContext.SaveChangesAsync().ConfigureAwait(false);

        AuditedAggregate entity = new() {
            Name = "Audited-A",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(entity);
        await unitOfWork.SaveChangesAsync(creator.Id).ConfigureAwait(false);
        entity.Name = "Audited-A-Updated";
        await unitOfWork.SaveChangesAsync(updater.Id).ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();

        AuditedAggregateAuditQueryService sut = new(database.DbContext);
        IReadOnlyList<AuditProjection> items = await sut.GetListAsync(
            query => new AuditProjection {
                Name = query.Entity.Name,
                CreatorName = query.Creator == null ? null : query.Creator.DisplayName,
                UpdaterName = query.Updater == null ? null : query.Updater.DisplayName
            }
        ).ConfigureAwait(false);

        AuditProjection actual = items.Single();
        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Name, Is.EqualTo("Audited-A-Updated"));
            Assert.That(actual.CreatorName, Is.EqualTo("Creator-A"));
            Assert.That(actual.UpdaterName, Is.EqualTo("Updater-A"));
            Assert.That(database.DbContext.ChangeTracker.Entries(), Is.Empty);
        }
    }

    [Test]
    public async Task GetListAsync_UserNotFound_UsesLeftJoinAndReturnsNullAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate entity = new() {
            Name = "Missing-User",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(entity);
        await unitOfWork.SaveChangesAsync(Guid.CreateVersion7()).ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();

        AuditedAggregateAuditQueryService sut = new(database.DbContext);
        IReadOnlyList<AuditProjection> items = await sut.GetListAsync(
            query => new AuditProjection {
                Name = query.Entity.Name,
                CreatorName = query.Creator == null ? null : query.Creator.DisplayName,
                UpdaterName = query.Updater == null ? null : query.Updater.DisplayName
            }
        ).ConfigureAwait(false);

        AuditProjection actual = items.Single();
        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Name, Is.EqualTo("Missing-User"));
            Assert.That(actual.CreatorName, Is.Null);
            Assert.That(actual.UpdaterName, Is.Null);
        }
    }

    [Test]
    public async Task GetListAsync_ProjectEntity_DoesNotTrackEntityInstancesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        TestUserEntity creator = new() {
            DisplayName = "Creator-B"
        };
        database.DbContext.TestUsers.Add(creator);
        await database.DbContext.SaveChangesAsync().ConfigureAwait(false);
        AuditedAggregate entity = new() {
            Name = "Tracked-Entity",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.Add(entity);
        await unitOfWork.SaveChangesAsync(creator.Id).ConfigureAwait(false);
        database.DbContext.ChangeTracker.Clear();

        AuditedAggregateAuditQueryService sut = new(database.DbContext);
        IReadOnlyList<AuditedAggregate> items = await sut.GetListAsync(query => query.Entity).ConfigureAwait(false);

        using (Assert.EnterMultipleScope()) {
            Assert.That(items.Single().Name, Is.EqualTo("Tracked-Entity"));
            Assert.That(database.DbContext.ChangeTracker.Entries(), Is.Empty);
        }
    }

    private sealed class AuditedAggregateAuditQueryService
        : AuditedQueryService<TestDbContext, AuditedAggregate, TestUserEntity> {
        protected override IQueryable<TestUserEntity> Users => DbContext.TestUsers;

        protected override System.Linq.Expressions.Expression<Func<AuditedAggregate, Guid>> CreatorIdSelector =>
            entity => entity.CreatorId;

        protected override System.Linq.Expressions.Expression<Func<AuditedAggregate, Guid?>>? UpdaterIdSelector =>
            entity => entity.UpdaterId;

        protected override System.Linq.Expressions.Expression<Func<TestUserEntity, Guid>> UserIdSelector =>
            user => user.Id;

        public AuditedAggregateAuditQueryService(TestDbContext dbContext)
            : base(dbContext) {
        }
    }

    private sealed class AuditProjection {
        public required string Name { get; init; }

        public string? CreatorName { get; init; }

        public string? UpdaterName { get; init; }
    }
}
