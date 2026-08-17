using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tests.TestDoubles;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Entities;

[TestFixture]
public sealed class AggregateRootTests {
    [Test]
    public void Constructor_DefaultAggregateRootId_CreatesVersion7Guid() {
        AuditedAggregate aggregate = new() {
            Name = "Account",
            ExternalKey = Guid.CreateVersion7()
        };

        Assert.That(aggregate.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(aggregate.Id.ToString("D")[14], Is.EqualTo('7'));
    }

    [Test]
    public void Events_AddAndClearEvents_KeepsDomainAndIntegrationEventsIndependent() {
        AuditedAggregate aggregate = new() {
            Name = "Account",
            ExternalKey = Guid.CreateVersion7()
        };

        aggregate.RaiseDomainEvent(Guid.CreateVersion7());
        aggregate.RaiseIntegrationEvent();
        aggregate.ClearDomainEvents();

        using (Assert.EnterMultipleScope()) {
            Assert.That(aggregate.DomainEvents, Is.Empty);
            Assert.That(aggregate.IntegrationEvents, Has.Count.EqualTo(1));
        }

        aggregate.ClearIntegrationEvents();

        Assert.That(aggregate.IntegrationEvents, Is.Empty);
    }

    [Test]
    public void DomainEvents_ReadOnlyView_RejectsMutationThroughCollectionInterface() {
        AuditedAggregate aggregate = new() {
            Name = "Account",
            ExternalKey = Guid.CreateVersion7()
        };
        aggregate.RaiseDomainEvent(Guid.CreateVersion7());

        ICollection<IDomainEvent> collection = (ICollection<IDomainEvent>)aggregate.DomainEvents;

        Assert.Throws<NotSupportedException>(
            () => collection.Add(new TestDomainEvent(aggregate.Id, Guid.CreateVersion7(), false))
        );
    }

    [Test]
    public void Equals_SameTypeAndSamePersistedId_ReturnsTrue() {
        Guid id = Guid.CreateVersion7();
        ComparableEntity left = new(id);
        ComparableEntity right = new(id);

        Assert.That(left, Is.EqualTo(right));
    }

    private sealed class ComparableEntity : CloudyWing.DomainKit.Entities.Entity<Guid> {
        public ComparableEntity(Guid id)
            : base(id) {
        }
    }
}
