using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class EventRecorder {
    public int IntegrationHandleAttempts { get; set; }

    public int IntegrationFailuresRemaining { get; set; }

    public List<IDomainEvent> DomainEvents { get; } = [];

    public List<IIntegrationEvent> IntegrationEvents { get; } = [];

    public List<Guid?> IntegrationEventTenantIds { get; } = [];
}
