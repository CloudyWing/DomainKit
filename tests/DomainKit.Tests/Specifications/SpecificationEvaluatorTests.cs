using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Specifications;
using CloudyWing.DomainKit.Tests.TestDoubles;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Specifications;

[TestFixture]
public sealed class SpecificationEvaluatorTests {
    [Test]
    public void ApplyFilter_WithSpecification_AppliesWhereClause() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name.StartsWith("A"));

        string[] actual = SpecificationEvaluator<AuditedAggregate>
            .ApplyFilter(query, specification)
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Alpha", "Astra" }));
    }

    [Test]
    public void ApplyPaged_WithPagedSpecification_AppliesOrderThenPagination() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        IPagedSpecification<AuditedAggregate> specification = PagedSpecification<AuditedAggregate>.Create(
            entity => entity.Name != "",
            entities => entities.OrderBy(entity => entity.Name),
            2,
            1
        );

        string[] actual = SpecificationEvaluator<AuditedAggregate>
            .ApplyPaged(query, specification)
            .Select(entity => entity.Name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Astra" }));
    }

    [Test]
    public void ApplyPaged_PageOffsetExceedsInt32_ReturnsEmptyPage() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        IPagedSpecification<AuditedAggregate> specification = PagedSpecification<AuditedAggregate>.Create(
            entity => entity.Name != "",
            entities => entities.OrderBy(entity => entity.Name),
            int.MaxValue,
            100
        );

        string[] actual = SpecificationEvaluator<AuditedAggregate>
            .ApplyPaged(query, specification)
            .Select(entity => entity.Name)
            .ToArray();

        Assert.That(actual, Is.Empty);
    }

    private static IQueryable<AuditedAggregate> CreateQuery() {
        return new[] {
            new AuditedAggregate {
                Name = "Beta",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Alpha",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Astra",
                ExternalKey = Guid.CreateVersion7()
            }
        }.AsQueryable();
    }
}
