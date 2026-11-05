using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

internal sealed class OutboxAdministrator<TDbContext> : IOutboxAdministrator where TDbContext : DbContext {
    private readonly TDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public OutboxAdministrator(TDbContext dbContext, TimeProvider timeProvider) {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<bool> RequeueDeadLetteredAsync(Guid id, CancellationToken cancellationToken = default) {
        DateTimeOffset now = timeProvider.GetUtcNow();
        int affected = await dbContext.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .Where(message => message.Id == id && message.Status == OutboxMessageStatus.DeadLettered)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, OutboxMessageStatus.Pending)
                    .SetProperty(message => message.RetryCount, 0)
                    .SetProperty(message => message.NextRetryOn, now)
                    .SetProperty(message => message.LockedBy, (string?)null)
                    .SetProperty(message => message.LockedUntil, (DateTimeOffset?)null),
                cancellationToken
            )
            .ConfigureAwait(false);

        return affected == 1;
    }

    public async Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default) {
        IQueryable<OutboxMessage> messages = dbContext.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .AsNoTracking();
        Dictionary<OutboxMessageStatus, int> counts = await messages
            .GroupBy(message => message.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken)
            .ConfigureAwait(false);
        List<DateTimeOffset> oldestPending = await messages
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .OrderBy(message => message.OccurredOn)
            .Select(message => message.OccurredOn)
            .Take(1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new OutboxStatistics {
            PendingCount = counts.GetValueOrDefault(OutboxMessageStatus.Pending),
            ProcessingCount = counts.GetValueOrDefault(OutboxMessageStatus.Processing),
            ProcessedCount = counts.GetValueOrDefault(OutboxMessageStatus.Processed),
            DeadLetteredCount = counts.GetValueOrDefault(OutboxMessageStatus.DeadLettered),
            OldestPendingOccurredOn = oldestPending.Count == 0 ? null : oldestPending[0]
        };
    }
}
