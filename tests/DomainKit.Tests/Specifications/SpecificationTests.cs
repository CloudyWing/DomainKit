using System.Linq.Expressions;
using System.Reflection;
using CloudyWing.DomainKit.Specifications;
using CloudyWing.DomainKit.Tests.TestDoubles;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Specifications;

[TestFixture]
public sealed class SpecificationTests {
    [Test]
    public void Specification_PublicProperties_DoNotExposePublicSetter() {
        PropertyInfo[] properties = typeof(Specification<AuditedAggregate>).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.That(properties.All(property => property.SetMethod is null || !property.SetMethod.IsPublic), Is.True);
    }

    [Test]
    public void Specification_GenericConstraints_DoNotRequireDefaultConstructorOrReferenceType() {
        Type[] openGenericTypes = [
            typeof(ISpecification<>),
            typeof(IPagedSpecification<>),
            typeof(Specification<>),
            typeof(PagedSpecification<>)
        ];

        using (Assert.EnterMultipleScope()) {
            foreach (Type openGenericType in openGenericTypes) {
                GenericParameterAttributes attributes = openGenericType.GetGenericArguments()[0].GenericParameterAttributes;

                Assert.That(
                    attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint),
                    Is.False,
                    $"{openGenericType.Name} should not require a default constructor."
                );
                Assert.That(
                    attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint),
                    Is.False,
                    $"{openGenericType.Name} should not require a reference type."
                );
            }
        }
    }

    [Test]
    public void ToExpression_ConstructedSpecification_ReturnsExpressionTree() {
        ISpecification<AuditedAggregate> sut = new Specification<AuditedAggregate>(entity => entity.Name == "Alpha");

        Assert.That(sut.ToExpression(), Is.InstanceOf<Expression<Func<AuditedAggregate, bool>>>());
    }
}
