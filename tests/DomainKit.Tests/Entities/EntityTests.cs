using System.Runtime.CompilerServices;
using CloudyWing.DomainKit.Entities;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Entities;

[TestFixture]
public sealed class EntityTests {
    [Test]
    public void GetHashCode_TransientEntity_UsesReferenceHash() {
        IntKeyEntity sut = new(0);

        int actual = sut.GetHashCode();

        Assert.That(actual, Is.EqualTo(RuntimeHelpers.GetHashCode(sut)));
    }

    [Test]
    public void Equals_TwoTransientEntities_ReturnsFalse() {
        IntKeyEntity left = new(0);
        IntKeyEntity right = new(0);

        bool actual = left.Equals(right);

        Assert.That(actual, Is.False);
    }

    [Test]
    public void Equals_TransientEntityWithItself_ReturnsTrue() {
        IntKeyEntity sut = new(0);

        bool actual = sut.Equals(sut);

        Assert.That(actual, Is.True);
    }

    [TestCase(1, 1, true)]
    [TestCase(1, 2, false)]
    public void Equals_PersistedEntities_ComparesTypeAndId(int leftId, int rightId, bool expected) {
        IntKeyEntity left = new(leftId);
        IntKeyEntity right = new(rightId);

        bool actual = left.Equals(right);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetHashCode_PersistedEntitiesWithSameId_ReturnsSameHash() {
        IntKeyEntity left = new(7);
        IntKeyEntity right = new(7);

        Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    }

    [Test]
    public void Equals_DifferentTypesWithSameId_ReturnsFalse() {
        IntKeyEntity left = new(7);
        OtherIntKeyEntity right = new(7);

        bool actual = left.Equals(right);

        Assert.That(actual, Is.False);
    }

    private sealed class IntKeyEntity : Entity<int> {
        public IntKeyEntity(int id)
            : base(id) {
        }
    }

    private sealed class OtherIntKeyEntity : Entity<int> {
        public OtherIntKeyEntity(int id)
            : base(id) {
        }
    }
}
