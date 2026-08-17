using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.ModelBuilder;

[TestFixture]
public sealed class DomainKitConventionsTests {
    [Test]
    public async Task ApplyDomainKitConventions_NonZeroOffsetWritten_ReadsBackAsUtcAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        DateTimeOffset occurredAt = new(2026, 6, 10, 15, 30, 0, TimeSpan.FromHours(8));
        DateTimeOffset completedAt = new(2026, 6, 10, 18, 0, 0, TimeSpan.FromHours(-5));
        TimestampEntity entity = new() {
            Name = "Convention",
            OccurredAt = occurredAt,
            CompletedAt = completedAt
        };
        database.DbContext.TimestampEntities.Add(entity);
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        TimestampEntity restored = await database.DbContext.TimestampEntities
            .AsNoTracking()
            .SingleAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(restored.OccurredAt.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(restored.OccurredAt, Is.EqualTo(occurredAt));
            Assert.That(restored.CompletedAt.HasValue, Is.True);
            Assert.That(restored.CompletedAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(restored.CompletedAt.Value, Is.EqualTo(completedAt));
        }
    }

    [Test]
    public async Task ApplyDomainKitConventions_NullNullableDateTimeOffset_RoundTripsAsNullAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        TimestampEntity entity = new() {
            Name = "NullCompleted",
            OccurredAt = DateTimeOffset.UtcNow,
            CompletedAt = null
        };
        database.DbContext.TimestampEntities.Add(entity);
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        TimestampEntity restored = await database.DbContext.TimestampEntities
            .AsNoTracking()
            .SingleAsync();

        Assert.That(restored.CompletedAt, Is.Null);
    }
}
