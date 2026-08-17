using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class ActivityStatusEntity : AggregateRootBase {
    public ActivityStatus Status { get; set; } = ActivityStatus.Draft;

    public ActivityStatus? NullableStatus { get; set; }
}
