using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class TestUserEntity : AggregateRootBase {
    public string DisplayName { get; set; } = "";
}
