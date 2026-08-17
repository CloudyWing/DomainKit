using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.ModelBuilder;

[TestFixture]
public sealed class SqlServerConventionTests {
    [Test]
    public void SqlServerModel_FullAuditAndRowVersionConventions_BuildExpectedMetadata() {
        DbContextOptions<SqlServerConventionDbContext> options =
            new DbContextOptionsBuilder<SqlServerConventionDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DomainKitConventionTests;Trusted_Connection=True;")
                .Options;
        using SqlServerConventionDbContext dbContext = new(options);

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(AuditedAggregate))
            ?? throw new AssertionException("找不到 AuditedAggregate 的模型定義。");
        IProperty rowVersionProperty = entityType.FindProperty(nameof(IHasRowVersion.RowVersion))
            ?? throw new AssertionException("找不到 RowVersion 的模型定義。");
        IProperty deletedTimeProperty = entityType.FindProperty(nameof(IHasDeletedTime.DeletedTime))
            ?? throw new AssertionException("找不到 DeletedTime 的模型定義。");

        using (Assert.EnterMultipleScope()) {
            Assert.That(rowVersionProperty.IsConcurrencyToken, Is.True);
            Assert.That(rowVersionProperty.IsNullable, Is.False);
            Assert.That(rowVersionProperty.ValueGenerated, Is.EqualTo(ValueGenerated.Never));
            Assert.That(deletedTimeProperty.IsNullable, Is.True);
            Assert.That(entityType.GetDeclaredQueryFilters(), Is.Not.Empty);
        }
    }

    [Test]
    public void SqlServerModel_AppManagedRowVersion_DoesNotSkipInterceptorStamp() {
        DbContextOptions<SqlServerConventionDbContext> options =
            new DbContextOptionsBuilder<SqlServerConventionDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DomainKitConventionTests;Trusted_Connection=True;")
                .Options;
        using SqlServerConventionDbContext dbContext = new(options);
        AuditedAggregate aggregate = new() {
            Name = "SQL Server row version",
            ExternalKey = Guid.CreateVersion7()
        };
        dbContext.Add(aggregate);
        PropertyEntry property = dbContext.Entry(aggregate).Property(nameof(IHasRowVersion.RowVersion));

        bool actual = RowVersionStampInterceptor.ShouldSkipStamp(property);

        Assert.That(actual, Is.False);
    }

    private sealed class SqlServerConventionDbContext : DbContext {
        public SqlServerConventionDbContext(DbContextOptions<SqlServerConventionDbContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<AuditedAggregate>(builder => {
                builder.ConfigureFullAuditedEntity();
                builder.ConfigureRowVersion();
                builder.Property(entity => entity.Name).IsRequired();
                builder.Property(entity => entity.ExternalKey).IsRequired();
            });
        }
    }
}
