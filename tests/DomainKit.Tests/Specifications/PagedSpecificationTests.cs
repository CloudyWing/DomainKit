using System.Reflection;
using CloudyWing.DomainKit.Specifications;
using CloudyWing.DomainKit.Tests.TestDoubles;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Specifications;

[TestFixture]
public sealed class PagedSpecificationTests {
    [Test]
    public void Create_WithPagination_StoresOrderAndPagingMetadata() {
        PagedSpecification<AuditedAggregate> specification = PagedSpecification<AuditedAggregate>.Create(
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name),
            2,
            3
        );
        string[] orderedNames = specification.OrderBy(CreateQuery()).Select(entity => entity.Name).ToArray();

        using (Assert.EnterMultipleScope()) {
            Assert.That(specification.HasPagination, Is.True);
            Assert.That(specification.PageNumber, Is.EqualTo(2));
            Assert.That(specification.PageSize, Is.EqualTo(3));
            Assert.That(orderedNames, Is.EqualTo(new[] { "Alpha", "Astra", "Beta" }));
        }
    }

    [Test]
    public void Create_WithoutPagination_SetsHasPaginationFalse() {
        PagedSpecification<AuditedAggregate> specification = PagedSpecification<AuditedAggregate>.Create(
            entity => entity.Name != "",
            query => query.OrderBy(entity => entity.Name)
        );

        using (Assert.EnterMultipleScope()) {
            Assert.That(specification.HasPagination, Is.False);
            Assert.That(specification.PageNumber, Is.EqualTo(0));
            Assert.That(specification.PageSize, Is.EqualTo(0));
        }
    }

    [Test]
    public void Create_WithNonPositivePaging_ThrowsArgumentOutOfRangeException() {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PagedSpecification<AuditedAggregate>.Create(
                entity => entity.Name != "",
                query => query.OrderBy(entity => entity.Name),
                0,
                1
            )
        );
    }

    [Test]
    public void PublicProperties_DoNotExposePublicSetter() {
        PropertyInfo[] properties = typeof(PagedSpecification<AuditedAggregate>).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.That(properties.All(property => property.SetMethod is null || !property.SetMethod.IsPublic), Is.True);
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
