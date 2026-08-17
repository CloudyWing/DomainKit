using CloudyWing.Enumeration.Abstractions;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class ActivityStatus : IntEnumeration<ActivityStatus> {
    public static ActivityStatus Draft { get; } = new(1, "Draft");

    public static ActivityStatus Active { get; } = new(2, "Active");

    public static ActivityStatus Archived { get; } = new(3, "Archived");

    private ActivityStatus(int value, string name)
        : base(value, name) {
    }
}
