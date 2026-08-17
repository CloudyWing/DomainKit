using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.ModelBuilder;

[TestFixture]
public sealed class UtcDateTimeConverterTests {
    [Test]
    public void Convert_NonZeroOffset_NormalizesToUtcDateTimeAndPreservesInstant() {
        UtcDateTimeConverter sut = new();
        DateTimeOffset source = new(2026, 6, 10, 15, 30, 0, TimeSpan.FromHours(8));

        DateTime stored = (DateTime)sut.ConvertToProvider(source)!;
        DateTimeOffset restored = (DateTimeOffset)sut.ConvertFromProvider(stored)!;

        using (Assert.EnterMultipleScope()) {
            Assert.That(stored.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(stored, Is.EqualTo(source.UtcDateTime));
            Assert.That(restored.UtcDateTime, Is.EqualTo(source.UtcDateTime));
            Assert.That(restored.Offset, Is.EqualTo(TimeSpan.Zero));
        }
    }

    [Test]
    public async Task SqliteQuery_DateTimeOffsetPredicate_RemainsConsistentAsync() {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        DbContextOptions<UtcDateTimeTestDbContext> options = new DbContextOptionsBuilder<UtcDateTimeTestDbContext>()
            .UseSqlite(connection)
            .Options;
        await using UtcDateTimeTestDbContext dbContext = new(options);
        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

        DateTimeOffset withinRange = new(2026, 6, 10, 15, 30, 0, TimeSpan.FromHours(8));
        DateTimeOffset outsideRange = withinRange.AddDays(-1);
        dbContext.TimestampEntities.AddRange(
            new TimestampEntity {
                Name = "Within",
                OccurredAt = withinRange,
                CompletedAt = withinRange.AddHours(1)
            },
            new TimestampEntity {
                Name = "Outside",
                OccurredAt = outsideRange,
                CompletedAt = outsideRange.AddHours(1)
            }
        );
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        dbContext.ChangeTracker.Clear();

        TimestampEntity restored = await dbContext.TimestampEntities
            .SingleAsync(entity => entity.Name == "Within")
            .ConfigureAwait(false);
        IReadOnlyList<string> actual = await dbContext.TimestampEntities
            .Where(entity => entity.OccurredAt >= withinRange && entity.CompletedAt <= withinRange.AddHours(1))
            .OrderBy(entity => entity.Name)
            .Select(entity => entity.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope()) {
            Assert.That(restored.OccurredAt.UtcDateTime, Is.EqualTo(withinRange.UtcDateTime));
            Assert.That(restored.OccurredAt.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(restored.CompletedAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(actual, Is.EqualTo(new[] { "Within" }));
        }
    }

    private sealed class UtcDateTimeTestDbContext : DbContext {
        public UtcDateTimeTestDbContext(DbContextOptions<UtcDateTimeTestDbContext> options)
            : base(options) {
        }

        public DbSet<TimestampEntity> TimestampEntities => Set<TimestampEntity>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
            configurationBuilder.ApplyDomainKitConventions(new UtcDateTimeConverter());
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<TimestampEntity>(
                builder => {
                    builder.ConfigureEntityBase();
                    builder.Property(entity => entity.Name).IsRequired();
                }
            );
        }
    }
}
