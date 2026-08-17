using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed record ReentrantIntegrationEvent(Guid AggregateId) : IIntegrationEvent {
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
