using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class ReentrantIntegrationEventHandler : IIntegrationEventHandler<ReentrantIntegrationEvent> {
    private readonly EventRecorder recorder;
    private readonly TestDbContext dbContext;
    private readonly IUnitOfWork unitOfWork;

    public ReentrantIntegrationEventHandler(
        EventRecorder recorder,
        TestDbContext dbContext,
        IUnitOfWork unitOfWork
    ) {
        this.recorder = recorder;
        this.dbContext = dbContext;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        ReentrantIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    ) {
        recorder.IntegrationEvents.Add(integrationEvent);
        if (recorder.IntegrationEvents.OfType<ReentrantIntegrationEvent>().Count() > 1) {
            throw new InvalidOperationException("Reentrant integration event was dispatched more than once.");
        }

        dbContext.HardDeleteEntities.Add(
            new HardDeleteEntity {
                Name = $"reentrant-{integrationEvent.AggregateId}"
            }
        );
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
