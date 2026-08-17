using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class SideEffectEntity : EntityBase {
    public required string Note { get; set; }
}
