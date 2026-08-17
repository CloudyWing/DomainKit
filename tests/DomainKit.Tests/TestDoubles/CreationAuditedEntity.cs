using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class CreationAuditedEntity : EntityBase,
    IHasCreatedTime,
    IHasCreator {
    public required string Name { get; set; }

    public DateTimeOffset CreatedTime { get; private set; }

    public Guid CreatorId { get; private set; }
}
