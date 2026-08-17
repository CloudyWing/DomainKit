using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class HardDeleteEntity : EntityBase {
    public required string Name { get; set; }
}
