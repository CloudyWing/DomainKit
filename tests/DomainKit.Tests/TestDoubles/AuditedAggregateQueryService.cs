using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class AuditedAggregateQueryService : QueryService<TestDbContext, AuditedAggregate> {
    public AuditedAggregateQueryService(TestDbContext dbContext)
        : base(dbContext) {
    }
}
