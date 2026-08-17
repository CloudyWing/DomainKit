using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed record FollowUpDomainEvent(Guid SideEffectId) : IDomainEvent {
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
