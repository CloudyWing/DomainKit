using CloudyWing.DomainKit.Events;
using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class FollowUpDomainEventHandler : IDomainEventHandler<FollowUpDomainEvent> {
    private readonly TestDbContext dbContext;
    private readonly EventRecorder recorder;

    public FollowUpDomainEventHandler(TestDbContext dbContext, EventRecorder recorder) {
        this.dbContext = dbContext;
        this.recorder = recorder;
    }

    public async Task HandleAsync(FollowUpDomainEvent domainEvent, CancellationToken cancellationToken = default) {
        recorder.DomainEvents.Add(domainEvent);

        SideEffectEntity? sideEffect = dbContext.SideEffects.Local
            .SingleOrDefault(entity => entity.Id == domainEvent.SideEffectId);
        sideEffect ??= await dbContext.SideEffects
            .SingleAsync(entity => entity.Id == domainEvent.SideEffectId, cancellationToken)
            .ConfigureAwait(false);
        sideEffect.Note = "follow-up";
    }
}
