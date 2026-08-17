using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed record TestDomainEvent(
    Guid AggregateId,
    Guid SideEffectId,
    bool RaiseFollowUp
) : IDomainEvent {
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
