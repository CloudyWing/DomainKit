using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events;

internal sealed class IntegrationEventContext : IIntegrationEventContext {
    public Guid EventId { get; private set; }

    public void SetEventId(Guid eventId) {
        EventId = eventId;
    }
}
