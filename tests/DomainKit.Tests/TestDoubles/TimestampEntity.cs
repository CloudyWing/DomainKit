using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class TimestampEntity : EntityBase {
    public required string Name { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
