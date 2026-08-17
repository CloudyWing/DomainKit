using System.Data.Common;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.ModelBuilder;

[TestFixture]
public sealed class EnumerationConventionTests {
    [Test]
    public async Task ApplyEnumerationConventions_RoundTripsEnumerationValuesAsync() {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);
        DbContextOptions<EnumerationConventionDbContext> options = new DbContextOptionsBuilder<EnumerationConventionDbContext>()
            .UseSqlite(connection)
            .Options;

        await using EnumerationConventionDbContext context = new(options);
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        ActivityStatusEntity entity = new() {
            Status = ActivityStatus.Active,
            NullableStatus = ActivityStatus.Archived
        };
        context.ActivityStatusEntities.Add(entity);
        await context.SaveChangesAsync().ConfigureAwait(false);
        context.ChangeTracker.Clear();

        ActivityStatusEntity actual = await context.ActivityStatusEntities.SingleAsync().ConfigureAwait(false);
        (int statusValue, int? nullableStatusValue) = await ReadStoredValuesAsync(connection).ConfigureAwait(false);

        using (Assert.EnterMultipleScope()) {
            Assert.That(statusValue, Is.EqualTo(ActivityStatus.Active.Value));
            Assert.That(nullableStatusValue, Is.EqualTo(ActivityStatus.Archived.Value));
            Assert.That(actual.Status, Is.SameAs(ActivityStatus.Active));
            Assert.That(actual.NullableStatus, Is.SameAs(ActivityStatus.Archived));
        }
    }

    [Test]
    public async Task ApplyEnumerationConventions_RoundTripsNullNullableEnumerationAsync() {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);
        DbContextOptions<EnumerationConventionDbContext> options = new DbContextOptionsBuilder<EnumerationConventionDbContext>()
            .UseSqlite(connection)
            .Options;

        await using EnumerationConventionDbContext context = new(options);
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        ActivityStatusEntity entity = new() {
            Status = ActivityStatus.Draft,
            NullableStatus = null
        };
        context.ActivityStatusEntities.Add(entity);
        await context.SaveChangesAsync().ConfigureAwait(false);
        context.ChangeTracker.Clear();

        ActivityStatusEntity actual = await context.ActivityStatusEntities.SingleAsync().ConfigureAwait(false);
        (int statusValue, int? nullableStatusValue) = await ReadStoredValuesAsync(connection).ConfigureAwait(false);

        using (Assert.EnterMultipleScope()) {
            Assert.That(statusValue, Is.EqualTo(ActivityStatus.Draft.Value));
            Assert.That(nullableStatusValue, Is.Null);
            Assert.That(actual.Status, Is.SameAs(ActivityStatus.Draft));
            Assert.That(actual.NullableStatus, Is.Null);
        }
    }

    [Test]
    public void WithoutEnumerationConventions_ModelCreationThrowsInvalidOperationException() {
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => {
                await using SqliteConnection connection = new("Data Source=:memory:");
                await connection.OpenAsync().ConfigureAwait(false);
                DbContextOptions<EnumerationWithoutConventionDbContext> options =
                    new DbContextOptionsBuilder<EnumerationWithoutConventionDbContext>()
                        .UseSqlite(connection)
                        .Options;

                await using EnumerationWithoutConventionDbContext context = new(options);
                await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
            }
        );
    }

    private static async Task<(int Status, int? NullableStatus)> ReadStoredValuesAsync(DbConnection connection) {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Status, NullableStatus FROM ActivityStatusEntities LIMIT 1";
        await using DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        await reader.ReadAsync().ConfigureAwait(false);

        return (
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1)
        );
    }

    private sealed class EnumerationConventionDbContext : DbContext {
        public EnumerationConventionDbContext(DbContextOptions<EnumerationConventionDbContext> options)
            : base(options) {
        }

        public DbSet<ActivityStatusEntity> ActivityStatusEntities => Set<ActivityStatusEntity>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
            configurationBuilder.ApplyDomainKitConventions()
                .ApplyEnumerationConventions(typeof(ActivityStatus).Assembly);
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<ActivityStatusEntity>(
                builder => {
                    builder.ConfigureEntityBase();
                    builder.Property(entity => entity.Status).IsRequired();
                    builder.Property(entity => entity.NullableStatus);
                }
            );
        }
    }

    private sealed class EnumerationWithoutConventionDbContext : DbContext {
        public EnumerationWithoutConventionDbContext(DbContextOptions<EnumerationWithoutConventionDbContext> options)
            : base(options) {
        }

        public DbSet<ActivityStatusEntity> ActivityStatusEntities => Set<ActivityStatusEntity>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
            configurationBuilder.ApplyDomainKitConventions();
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<ActivityStatusEntity>(
                builder => {
                    builder.ConfigureEntityBase();
                    builder.Property(entity => entity.Status).IsRequired();
                    builder.Property(entity => entity.NullableStatus);
                }
            );
        }
    }
}
