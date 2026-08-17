using CloudyWing.DomainKit.Events;
using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent> {
    private readonly TestDbContext dbContext;
    private readonly EventRecorder recorder;

    public TestDomainEventHandler(TestDbContext dbContext, EventRecorder recorder) {
        this.dbContext = dbContext;
        this.recorder = recorder;
    }

    public async Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default) {
        recorder.DomainEvents.Add(domainEvent);

        SideEffectEntity? sideEffect = dbContext.SideEffects.Local
            .SingleOrDefault(entity => entity.Id == domainEvent.SideEffectId);
        sideEffect ??= await dbContext.SideEffects
            .SingleAsync(entity => entity.Id == domainEvent.SideEffectId, cancellationToken)
            .ConfigureAwait(false);
        sideEffect.Note = "handled";

        if (domainEvent.RaiseFollowUp) {
            AuditedAggregate aggregate = dbContext.AuditedAggregates.Local
                .Single(entity => entity.Id == domainEvent.AggregateId);
            aggregate.RaiseFollowUpDomainEvent(domainEvent.SideEffectId);
        }
    }
}
