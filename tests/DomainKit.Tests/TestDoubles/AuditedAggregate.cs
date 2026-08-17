using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class AuditedAggregate : AggregateRootBase,
    IHasCreatedTime,
    IHasUpdatedTime,
    IHasCreator,
    IHasUpdater,
    IHasDeletedTime,
    IHasDeleter,
    IHasExternalKey<Guid>,
    IHasRowVersion {
    public required string Name { get; set; }

    public Guid ExternalKey { get; set; }

    public byte[] RowVersion { get; private set; } = [];

    public DateTimeOffset CreatedTime { get; private set; }

    public DateTimeOffset UpdatedTime { get; private set; }

    public Guid CreatorId { get; private set; }

    public Guid UpdaterId { get; private set; }

    public DateTimeOffset? DeletedTime { get; private set; }

    public Guid? DeleterId { get; private set; }

    public void RaiseDomainEvent(Guid sideEffectId, bool raiseFollowUp = false) {
        AddDomainEvent(new TestDomainEvent(Id, sideEffectId, raiseFollowUp));
    }

    public void RaiseFollowUpDomainEvent(Guid sideEffectId) {
        AddDomainEvent(new FollowUpDomainEvent(sideEffectId));
    }

    public void RaiseIntegrationEvent() {
        AddIntegrationEvent(new TestIntegrationEvent(Id));
    }

    public void RaiseReentrantIntegrationEvent() {
        AddIntegrationEvent(new ReentrantIntegrationEvent(Id));
    }
}
