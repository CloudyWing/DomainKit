using System.Reflection;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Paging;
using CloudyWing.DomainKit.Specifications;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Specifications;

[TestFixture]
public sealed class QueryServiceSpecificationTests {
    [Test]
    public async Task AnyAndCountAsync_WithSpecification_MatchDiscreteOverloadsAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "A");

        bool actualAny = await sut.AnyAsync(specification);
        int actualCount = await sut.CountAsync(specification);
        bool expectedAny = await sut.AnyAsync(entity => entity.Name != "A");
        int expectedCount = await sut.CountAsync(entity => entity.Name != "A");

        using (Assert.EnterMultipleScope()) {
            Assert.That(actualAny, Is.EqualTo(expectedAny));
            Assert.That(actualCount, Is.EqualTo(expectedCount));
        }
    }

    [Test]
    public async Task GetListAsync_WithSpecification_MatchesDiscreteOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "B");

        IReadOnlyList<string> actual = await sut.GetListAsync(
            entity => entity.Name,
            specification,
            query => query.OrderBy(entity => entity.Name)
        );
        IReadOnlyList<string> expected = await sut.GetListAsync(
            entity => entity.Name,
            entity => entity.Name != "B",
            query => query.OrderBy(entity => entity.Name)
        );

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetSingleOrDefaultAsync_WithSpecification_MatchesPredicateOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name == "A");

        string? actual = await sut.GetSingleOrDefaultAsync(specification, entity => entity.Name);
        string? expected = await sut.GetSingleOrDefaultAsync(entity => entity.Name == "A", entity => entity.Name);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetSingleAsync_WithSpecification_MatchesPredicateOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name == "A");

        string actual = await sut.GetSingleAsync(specification, entity => entity.Name);
        string expected = await sut.GetSingleAsync(entity => entity.Name == "A", entity => entity.Name);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetFirstOrDefaultAsync_WithSpecification_MatchesPredicateOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "Z");

        string? actual = await sut.GetFirstOrDefaultAsync(
            entity => entity.Name,
            specification,
            query => query.OrderByDescending(entity => entity.Name)
        );
        string? expected = await sut.GetFirstOrDefaultAsync(
            entity => entity.Name,
            entity => entity.Name != "Z",
            query => query.OrderByDescending(entity => entity.Name)
        );

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetFirstAsync_WithSpecification_MatchesPredicateOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "Z");

        string actual = await sut.GetFirstAsync(
            entity => entity.Name,
            specification,
            query => query.OrderByDescending(entity => entity.Name)
        );
        string expected = await sut.GetFirstAsync(
            entity => entity.Name,
            entity => entity.Name != "Z",
            query => query.OrderByDescending(entity => entity.Name)
        );

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetPagedListAsync_WithPagedSpecification_MatchesDiscreteOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext) {
            IgnoreSoftDeletedFilter = true
        };
        IPagedSpecification<AuditedAggregate> specification = PagedSpecification<AuditedAggregate>.Create(
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name),
            2,
            2
        );

        PagedList<string> actual = await sut.GetPagedListAsync(entity => entity.Name, specification);
        PagedList<string> expected = await sut.GetPagedListAsync(
            2,
            2,
            entity => entity.Name,
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name)
        );

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Items, Is.EqualTo(expected.Items));
            Assert.That(actual.Metadata.TotalCount, Is.EqualTo(expected.Metadata.TotalCount));
            Assert.That(actual.Metadata.TotalPages, Is.EqualTo(expected.Metadata.TotalPages));
        }
    }

    [Test]
    public async Task GetListAsync_WithSpecification_ReturnsReadOnlyListInsteadOfPagedListAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);

        object actual = await sut.GetListAsync(
            entity => entity.Name,
            new Specification<AuditedAggregate>(entity => entity.Name != "")
        );

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual, Is.AssignableTo<IReadOnlyList<string>>());
            Assert.That(actual, Is.Not.InstanceOf<PagedList<string>>());
        }
    }

    [Test]
    public async Task GetListAsync_ExtensionOverload_MatchesSpecificationOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "B");

        IReadOnlyList<string> actual = await QueryServiceSpecificationExtensions.GetListAsync(
            sut,
            entity => entity.Name,
            entity => entity.Name != "B",
            query => query.OrderBy(entity => entity.Name)
        );
        IReadOnlyList<string> expected = await sut.GetListAsync(
            entity => entity.Name,
            specification,
            query => query.OrderBy(entity => entity.Name)
        );

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetListAsync_TakeExtension_ReturnsLeadingRowsAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext) {
            IgnoreSoftDeletedFilter = true
        };

        IReadOnlyList<string> actual = await QueryServiceSpecificationExtensions.GetListAsync(
            sut,
            entity => entity.Name,
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name),
            2
        );

        Assert.That(actual, Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public async Task GetPagedListAsync_ExtensionOverload_MatchesSpecificationOverloadAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext) {
            IgnoreSoftDeletedFilter = true
        };
        IPagedSpecification<AuditedAggregate> specification = PagedSpecification<AuditedAggregate>.Create(
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name),
            2,
            2
        );

        PagedList<string> actual = await QueryServiceSpecificationExtensions.GetPagedListAsync(
            sut,
            entity => entity.Name,
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name),
            2,
            2
        );
        PagedList<string> expected = await sut.GetPagedListAsync(entity => entity.Name, specification);

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.Items, Is.EqualTo(expected.Items));
            Assert.That(actual.Metadata.TotalCount, Is.EqualTo(expected.Metadata.TotalCount));
            Assert.That(actual.Metadata.TotalPages, Is.EqualTo(expected.Metadata.TotalPages));
        }
    }

    [Test]
    public async Task IgnoreSoftDeletedFilter_WithSpecificationOverloads_IncludesDeletedRowsAsync() {
        await using TestDatabase database = await CreateSeededDatabaseAsync();
        AuditedAggregateQueryService sut = new(database.DbContext);
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name != "");

        int visibleCount = await sut.CountAsync(specification);
        sut.IgnoreSoftDeletedFilter = true;
        int totalCount = await sut.CountAsync(specification);

        using (Assert.EnterMultipleScope()) {
            Assert.That(visibleCount, Is.EqualTo(2));
            Assert.That(totalCount, Is.EqualTo(3));
        }
    }

    [Test]
    public void Repository_PublicMethods_DoNotAcceptSpecificationParameters() {
        MethodInfo[] methods = typeof(Repository<TestDbContext, AuditedAggregate, Guid>).GetMethods(BindingFlags.Instance | BindingFlags.Public);

        Assert.That(
            methods
                .SelectMany(method => method.GetParameters())
                .All(parameter => !IsSpecificationParameter(parameter.ParameterType)),
            Is.True
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

    private static bool IsSpecificationParameter(Type parameterType) {
        return parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() is var definition
            && (definition == typeof(ISpecification<>) || definition == typeof(IPagedSpecification<>));
    }
}
