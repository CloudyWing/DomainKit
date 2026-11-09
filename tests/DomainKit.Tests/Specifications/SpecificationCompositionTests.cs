using CloudyWing.DomainKit.Specifications;
using CloudyWing.DomainKit.Tests.TestDoubles;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Specifications;

[TestFixture]
public sealed class SpecificationCompositionTests {
    [Test]
    public void And_WithTwoSpecifications_ReturnsIntersection() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate> startsWithA = new Specification<AuditedAggregate>(entity => entity.Name.StartsWith("A"));
        ISpecification<AuditedAggregate> endsWithA = new Specification<AuditedAggregate>(entity => entity.Name.EndsWith("a"));

        string[] actual = query
            .Where(startsWithA.And(endsWithA).ToExpression())
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Alpha", "Astra" }));
    }

    [Test]
    public void Or_WithTwoSpecifications_ReturnsUnion() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate> startsWithA = new Specification<AuditedAggregate>(entity => entity.Name.StartsWith("A"));
        ISpecification<AuditedAggregate> nameIsBeta = new Specification<AuditedAggregate>(entity => entity.Name == "Beta");

        string[] actual = query
            .Where(startsWithA.Or(nameIsBeta).ToExpression())
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Alpha", "Astra", "Beta" }));
    }

    [Test]
    public void True_AnyEntity_MatchesAllRows() {
        IQueryable<AuditedAggregate> query = CreateQuery();

        int actual = query.Count(Specification<AuditedAggregate>.True.ToExpression());

        Assert.That(actual, Is.EqualTo(3));
    }

    [Test]
    public void False_AnyEntity_MatchesNoRows() {
        IQueryable<AuditedAggregate> query = CreateQuery();

        int actual = query.Count(Specification<AuditedAggregate>.False.ToExpression());

        Assert.That(actual, Is.Zero);
    }

    [Test]
    public void AllOf_MultipleSpecifications_ReturnsIntersection() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate>[] specifications = [
            new Specification<AuditedAggregate>(entity => entity.Name.StartsWith("A")),
            new Specification<AuditedAggregate>(entity => entity.Name.EndsWith("a")),
            new Specification<AuditedAggregate>(entity => entity.Name.Contains("lph"))
        ];

        string[] actual = query
            .Where(specifications.AllOf().ToExpression())
            .Select(entity => entity.Name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Alpha" }));
    }

    [Test]
    public void AnyOf_MultipleSpecifications_ReturnsUnion() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate>[] specifications = [
            new Specification<AuditedAggregate>(entity => entity.Name == "Alpha"),
            new Specification<AuditedAggregate>(entity => entity.Name == "Beta")
        ];

        string[] actual = query
            .Where(specifications.AnyOf().ToExpression())
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Alpha", "Beta" }));
    }

    [Test]
    public void AllOf_EmptySequence_ReturnsTrueSpecification() {
        ISpecification<AuditedAggregate> actual = Array.Empty<ISpecification<AuditedAggregate>>().AllOf();

        Assert.That(actual, Is.SameAs(Specification<AuditedAggregate>.True));
    }

    [Test]
    public void AnyOf_EmptySequence_ReturnsFalseSpecification() {
        ISpecification<AuditedAggregate> actual = Array.Empty<ISpecification<AuditedAggregate>>().AnyOf();

        Assert.That(actual, Is.SameAs(Specification<AuditedAggregate>.False));
    }

    [Test]
    public void AllOf_NullElement_ThrowsArgumentException() {
        ISpecification<AuditedAggregate>[] specifications = [null!];

        Assert.Throws<ArgumentException>(() => specifications.AllOf());
    }

    [Test]
    public void Not_WithSpecification_ReturnsComplement() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate> startsWithA = new Specification<AuditedAggregate>(entity => entity.Name.StartsWith("A"));

        string[] actual = query
            .Where(startsWithA.Not().ToExpression())
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Beta" }));
    }

    [Test]
    public void And_ComposedSpecification_ReturnsNewInstance() {
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => entity.Name.StartsWith("A"));
        ISpecification<AuditedAggregate> combined = specification.And(entity => entity.Name.EndsWith("a"));

        Assert.That(ReferenceEquals(specification, combined), Is.False);
    }

    [Test]
    public void AndIfHasValue_EmptyString_DoesNotAppendPredicate() {
        IQueryable<AuditedAggregate> query = CreateQuery();
        ISpecification<AuditedAggregate> specification = new Specification<AuditedAggregate>(entity => true);

        string[] actual = query
            .Where(
                specification
                    .AndIfHasValue("", value => entity => entity.Name == value)
                    .ToExpression()
            )
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(actual, Is.EqualTo(new[] { "Alpha", "Astra", "Beta" }));
    }

    private static IQueryable<AuditedAggregate> CreateQuery() {
        return new[] {
            new AuditedAggregate {
                Name = "Alpha",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Astra",
                ExternalKey = Guid.CreateVersion7()
            },
            new AuditedAggregate {
                Name = "Beta",
                ExternalKey = Guid.CreateVersion7()
            }
        }.AsQueryable();
    }
}
