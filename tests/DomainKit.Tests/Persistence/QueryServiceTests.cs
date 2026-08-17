using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Paging;
using CloudyWing.DomainKit.Specifications;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class QueryServiceTests {
    [Test]
    public async Task CountAsync_DefaultQuery_ExcludesSoftDeletedRowsAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);

        int visibleCount = await sut.CountAsync();
        sut.IgnoreSoftDeletedFilter = true;
        int totalCount = await sut.CountAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(visibleCount, Is.EqualTo(2));
            Assert.That(totalCount, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task GetListAsync_WithProjectionAndOrder_ReturnsProjectedRowsAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);

        IReadOnlyList<string> names = await sut.GetListAsync(
            entity => entity.Name,
            orderBy: query => query.OrderBy(entity => entity.Name)
        );

        Assert.That(names, Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public async Task GetPagedListAsync_WithProjection_ReturnsPageAndMetadataAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext) {
            IgnoreSoftDeletedFilter = true
        };

        PagedList<string> page = await sut.GetPagedListAsync(
            2,
            2,
            entity => entity.Name,
            orderBy: query => query.OrderBy(entity => entity.Name)
        );

        using (Assert.EnterMultipleScope()) {
            Assert.That(page.Items, Is.EqualTo(new[] { "C" }));
            Assert.That(page.Metadata.TotalCount, Is.EqualTo(3));
            Assert.That(page.Metadata.TotalPages, Is.EqualTo(2));
            Assert.That(page.Metadata.HasPreviousPage, Is.True);
            Assert.That(page.Metadata.HasNextPage, Is.False);
        }
    }

    [Test]
    public async Task GetFirstAsync_WithMultipleMatches_ReturnsOrderedFirstRowAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext) {
            IgnoreSoftDeletedFilter = true
        };

        string actual = await sut.GetFirstAsync(
            entity => entity.Name,
            orderBy: query => query.OrderBy(entity => entity.Name)
        );

        Assert.That(actual, Is.EqualTo("A"));
    }

    [Test]
    public async Task GetFirstOrDefaultAsync_NoMatchingRow_ReturnsNullAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);

        string? actual = await sut.GetFirstOrDefaultAsync(
            entity => entity.Name,
            entity => entity.Name == "Z",
            query => query.OrderBy(entity => entity.Name)
        );

        Assert.That(actual, Is.Null);
    }

    [Test]
    public void GetSingleAsync_WithMultipleMatches_ThrowsInvalidOperationException() {
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => {
                await using TestDatabase database = await CreateSeededDatabaseAsync();
                AuditedAggregateQueryService sut = new(database.DbContext) {
                    IgnoreSoftDeletedFilter = true
                };

                await sut.GetSingleAsync(entity => entity.Name != "Z", entity => entity.Name);
            }
        );
    }

    [Test]
    public async Task GetGroupedListAsync_WithHavingAndOrder_PushesGroupingToSqlAsync() {
        List<string> sqlLogs = [];
        await using TestDatabase database = await CreateGroupedDatabaseAsync(
            builder => builder.LogTo(message => sqlLogs.Add(message), LogLevel.Information)
        );
        AuditedAggregateQueryService sut = new(database.DbContext);

        IReadOnlyList<NameCountRow> actual = await sut.GetGroupedListAsync(
            entity => entity.Name,
            group => new NameCountRow {
                Name = group.Key,
                Count = group.Count()
            },
            havingPredicate: row => row.Count >= 2,
            orderBy: query => query.OrderByDescending(row => row.Name)
        );

        string combinedLogs = string.Join(Environment.NewLine, sqlLogs);

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Select(row => (row.Name, row.Count)), Is.EqualTo(new[] { ("Gamma", 2), ("Alpha", 2) }));
            Assert.That(combinedLogs, Does.Contain("GROUP BY"));
        }
    }

    [Test]
    public async Task GetGroupedListAsync_WithSpecification_MatchesPredicateOverloadAsync() {
        await using TestDatabase database = await CreateGroupedDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "Beta");

        IReadOnlyList<NameCountRow> actual = await sut.GetGroupedListAsync(
            entity => entity.Name,
            group => new NameCountRow {
                Name = group.Key,
                Count = group.Count()
            },
            specification,
            havingPredicate: row => row.Count >= 2,
            orderBy: query => query.OrderBy(row => row.Name)
        );
        IReadOnlyList<NameCountRow> expected = await sut.GetGroupedListAsync(
            entity => entity.Name,
            group => new NameCountRow {
                Name = group.Key,
                Count = group.Count()
            },
            entity => entity.Name != "Beta",
            havingPredicate: row => row.Count >= 2,
            orderBy: query => query.OrderBy(row => row.Name)
        );

        Assert.That(
            actual.Select(row => (row.Name, row.Count)),
            Is.EqualTo(expected.Select(row => (row.Name, row.Count)))
        );
    }

    private static async Task<TestDatabase> CreateSeededDatabaseAsync() {
        TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate deleted = new() {
            Name = "C",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.AddRange(
            new AuditedAggregate {
                Name = "B",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "A",
                ExternalKey = Guid.CreateVersion7()
            },
            deleted
        );
        await unitOfWork.SaveChangesAsync();
        database.DbContext.Entry(deleted).Property(nameof(deleted.DeletedTime)).CurrentValue = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        return database;
    }

    private static async Task<TestDatabase> CreateGroupedDatabaseAsync(
        Action<DbContextOptionsBuilder>? configureDbContext = null
    ) {
        TestDatabase database = await TestDatabase.CreateAsync(configureDbContext: configureDbContext);
        IUnitOfWork unitOfWork = database.Services.GetRequiredService<IUnitOfWork>();
        AuditedAggregate deleted = new() {
            Name = "Gamma",
            ExternalKey = Guid.CreateVersion7()
        };
        database.DbContext.AuditedAggregates.AddRange(
            new AuditedAggregate {
                Name = "Alpha",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Alpha",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Beta",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Gamma",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Gamma",
                ExternalKey = Guid.CreateVersion7()
            },
            deleted
        );
        await unitOfWork.SaveChangesAsync();
        database.DbContext.Entry(deleted).Property(nameof(deleted.DeletedTime)).CurrentValue = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        return database;
    }

    private sealed class NameCountRow {
        public required string Name { get; init; }

        public int Count { get; init; }
    }
}
