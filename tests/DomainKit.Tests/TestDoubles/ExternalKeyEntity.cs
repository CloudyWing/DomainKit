using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class ExternalKeyEntity : EntityBase, IHasExternalKey<string> {
    public required string ExternalKey { get; set; }

    public required string Name { get; set; }
}
