using CloudyWing.DomainKit.Events;
using CloudyWing.DomainKit.Tenancy;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent> {
    private readonly EventRecorder recorder;
    private readonly ICurrentTenant? currentTenant;

    public TestIntegrationEventHandler(EventRecorder recorder, ICurrentTenant? currentTenant = null) {
        this.recorder = recorder;
        this.currentTenant = currentTenant;
    }

    public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default) {
        recorder.IntegrationHandleAttempts++;
        if (recorder.IntegrationFailuresRemaining > 0) {
            recorder.IntegrationFailuresRemaining--;
            throw new InvalidOperationException("Simulated integration handler failure.");
        }

        recorder.IntegrationEvents.Add(integrationEvent);
        recorder.IntegrationEventTenantIds.Add(currentTenant?.TenantId);

        return Task.CompletedTask;
    }
}
