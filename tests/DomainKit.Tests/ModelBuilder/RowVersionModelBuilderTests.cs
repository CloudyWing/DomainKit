using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.ModelBuilder;

[TestFixture]
public sealed class RowVersionModelBuilderTests {
    [Test]
    public async Task ConfigureRowVersion_IHasRowVersionEntity_SetsRequiredConcurrencyTokenAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IEntityType entityType = database.DbContext.Model.FindEntityType(typeof(AuditedAggregate))
            ?? throw new AssertionException("找不到 AuditedAggregate 的模型定義。");
        IProperty property = entityType.FindProperty(nameof(IHasRowVersion.RowVersion))
            ?? throw new AssertionException("找不到 RowVersion 的模型定義。");

        using (Assert.EnterMultipleScope()) {
            Assert.That(property.IsConcurrencyToken, Is.True);
            Assert.That(property.IsNullable, Is.False);
        }
    }

    [Test]
    public async Task ShouldSkipStamp_NativeRowVersionProperty_ReturnsTrueAsync() {
        DbContextOptions<NativeRowVersionDbContext> options = new DbContextOptionsBuilder<NativeRowVersionDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using NativeRowVersionDbContext context = new(options);
        AuditedAggregate aggregate = new() {
            Name = "Native rowversion",
            ExternalKey = Guid.CreateVersion7()
        };
        context.Add(aggregate);
        PropertyEntry property = context.Entry(aggregate).Property(nameof(IHasRowVersion.RowVersion));

        bool actual = RowVersionStampInterceptor.ShouldSkipStamp(property);

        Assert.That(actual, Is.True);
    }

    private sealed class NativeRowVersionDbContext : DbContext {
        public NativeRowVersionDbContext(DbContextOptions<NativeRowVersionDbContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<AuditedAggregate>(builder => {
                builder.HasKey(entity => entity.Id);
                builder.Property(entity => entity.RowVersion).IsRowVersion();
            });
        }
    }
}
