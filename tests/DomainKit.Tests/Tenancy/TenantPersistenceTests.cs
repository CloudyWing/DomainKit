using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.EntityFrameworkCore.Tenancy;
using CloudyWing.DomainKit.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace CloudyWing.DomainKit.Tests.Tenancy;

[TestFixture]
public sealed class TenantPersistenceTests {
    [Test]
    public async Task SaveChangesAsync_TenantContext_StampsAndFiltersRowsAsync() {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        AmbientCurrentTenant currentTenant = new();
        DbContextOptions<TenantDbContext> options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new TenantStampInterceptor(currentTenant))
            .Options;
        await using TenantDbContext dbContext = new(options, currentTenant, applyTenantConventions: true);
        await dbContext.Database.EnsureCreatedAsync();
        Guid tenantAId = Guid.CreateVersion7();
        Guid tenantBId = Guid.CreateVersion7();

        using (currentTenant.Change(tenantAId)) {
            dbContext.Entities.Add(new TenantEntity(Guid.CreateVersion7(), "Tenant A"));
            await dbContext.SaveChangesAsync();
        }

        dbContext.ChangeTracker.Clear();
        using (currentTenant.Change(tenantBId)) {
            Assert.That(await dbContext.Entities.CountAsync(), Is.Zero);
            dbContext.Entities.Add(new TenantEntity(Guid.CreateVersion7(), "Tenant B"));
            await dbContext.SaveChangesAsync();
        }

        dbContext.ChangeTracker.Clear();
        using (currentTenant.Change(tenantAId)) {
            TenantEntity stored = await dbContext.Entities.SingleAsync();

            using (Assert.EnterMultipleScope()) {
                Assert.That(stored.Name, Is.EqualTo("Tenant A"));
                Assert.That(dbContext.Entry(stored).Property<Guid>("TenantId").CurrentValue, Is.EqualTo(tenantAId));
            }
        }
    }

    [Test]
    public async Task SaveChangesAsync_MissingTenantContext_ThrowsInvalidOperationExceptionAsync() {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        AmbientCurrentTenant currentTenant = new();
        DbContextOptions<TenantDbContext> options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new TenantStampInterceptor(currentTenant))
            .Options;
        await using TenantDbContext dbContext = new(options, currentTenant, applyTenantConventions: true);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Entities.Add(new TenantEntity(Guid.CreateVersion7(), "Missing"));

        InvalidOperationException? exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dbContext.SaveChangesAsync()
        );

        Assert.That(exception!.Message, Does.Contain("缺少租戶脈絡"));
    }

    [Test]
    public void ApplyTenantConventions_ExistingUniqueIndex_PrependsTenantProperty() {
        AmbientCurrentTenant currentTenant = new();
        DbContextOptions<TenantDbContext> options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using TenantDbContext dbContext = new(options, currentTenant, applyTenantConventions: true);

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TenantEntity))!;
        IIndex index = entityType.GetIndexes().Single();

        using (Assert.EnterMultipleScope()) {
            Assert.That(entityType.FindProperty("TenantId"), Is.Not.Null);
            Assert.That(index.IsUnique, Is.True);
            Assert.That(index.Properties.Select(property => property.Name), Is.EqualTo(["TenantId", "ExternalKey"]));
        }
    }

    [Test]
    public void ApplyTenantConventions_MarkedBusinessKey_PrependsTenantPropertyToPrimaryKey() {
        AmbientCurrentTenant currentTenant = new();
        DbContextOptions<TenantBusinessKeyDbContext> options =
            new DbContextOptionsBuilder<TenantBusinessKeyDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
        using TenantBusinessKeyDbContext dbContext = new(options, currentTenant);

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TenantBusinessKeyEntity))!;
        IKey primaryKey = entityType.FindPrimaryKey()!;

        Assert.That(primaryKey.Properties.Select(property => property.Name), Is.EqualTo(["TenantId", "Key"]));
    }

    [Test]
    public void OnModelCreating_WithoutTenantConventions_DoesNotAddTenantProperty() {
        DbContextOptions<NoTenantDbContext> options = new DbContextOptionsBuilder<NoTenantDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using NoTenantDbContext dbContext = new(options);

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TenantEntity))!;

        Assert.That(entityType.FindProperty("TenantId"), Is.Null);
    }

    [Test]
    public void ApplyTenantConventions_DerivedEntity_AppliesFilterOnlyToRootEntity() {
        AmbientCurrentTenant currentTenant = new();
        DbContextOptions<InheritanceTenantDbContext> options =
            new DbContextOptionsBuilder<InheritanceTenantDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
        using InheritanceTenantDbContext dbContext = new(options, currentTenant);

        IEntityType rootType = dbContext.Model.FindEntityType(typeof(InheritanceRoot))!;
        IEntityType derivedType = dbContext.Model.FindEntityType(typeof(InheritanceDerived))!;

        using (Assert.EnterMultipleScope()) {
            Assert.That(rootType.GetDeclaredQueryFilters(), Is.Not.Empty);
            Assert.That(derivedType.GetDeclaredQueryFilters(), Is.Empty);
            Assert.That(derivedType.FindProperty("TenantId"), Is.Not.Null);
        }
    }

    private sealed class InheritanceTenantDbContext : DbContext, ITenantDbContext {
        private readonly ICurrentTenant currentTenant;

        public InheritanceTenantDbContext(
            DbContextOptions<InheritanceTenantDbContext> options,
            ICurrentTenant currentTenant
        ) : base(options) {
            this.currentTenant = currentTenant;
        }

        public Guid? CurrentTenantId => currentTenant.TenantId;

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<InheritanceRoot>().HasKey(entity => entity.Id);
            modelBuilder.Entity<InheritanceDerived>();
            modelBuilder.ApplyTenantConventions(this);
        }
    }

    private sealed class NoTenantDbContext : DbContext {
        public NoTenantDbContext(DbContextOptions<NoTenantDbContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<TenantEntity>(
                builder => {
                    builder.HasKey(entity => entity.Id);
                    builder.HasIndex(entity => entity.ExternalKey).IsUnique();
                    builder.Property(entity => entity.Name).IsRequired();
                }
            );
        }
    }

    private sealed class TenantBusinessKeyDbContext : DbContext, ITenantDbContext {
        private readonly ICurrentTenant currentTenant;

        public TenantBusinessKeyDbContext(
            DbContextOptions<TenantBusinessKeyDbContext> options,
            ICurrentTenant currentTenant
        ) : base(options) {
            this.currentTenant = currentTenant;
        }

        public Guid? CurrentTenantId => currentTenant.TenantId;

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<TenantBusinessKeyEntity>().HasKey(entity => entity.Key);
            modelBuilder.HasTenantPrimaryKey<TenantBusinessKeyEntity>(nameof(TenantBusinessKeyEntity.Key));
            modelBuilder.ApplyTenantConventions(this);
        }
    }

    private sealed class TenantDbContext : DbContext, ITenantDbContext {
        private readonly bool applyTenantConventions;
        private readonly ICurrentTenant currentTenant;

        public TenantDbContext(
            DbContextOptions<TenantDbContext> options,
            ICurrentTenant currentTenant,
            bool applyTenantConventions
        ) : base(options) {
            this.currentTenant = currentTenant;
            this.applyTenantConventions = applyTenantConventions;
        }

        public DbSet<TenantEntity> Entities => Set<TenantEntity>();

        public Guid? CurrentTenantId => currentTenant.TenantId;

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<TenantEntity>(
                builder => {
                    builder.HasKey(entity => entity.Id);
                    builder.HasIndex(entity => entity.ExternalKey).IsUnique();
                    builder.Property(entity => entity.Name).IsRequired();
                    builder.HasQueryFilter(entity => !entity.IsDeleted);
                }
            );
            if (applyTenantConventions) {
                modelBuilder.ApplyTenantConventions(this);
            }
        }
    }

    private sealed class TenantEntity {
        private TenantEntity() {
        }

        [SetsRequiredMembers]
        public TenantEntity(Guid externalKey, string name) {
            ExternalKey = externalKey;
            Name = name;
        }

        public Guid Id { get; private set; } = Guid.CreateVersion7();

        public Guid ExternalKey { get; private set; }

        public required string Name { get; set; }

        public bool IsDeleted { get; private set; }
    }

    private sealed class TenantBusinessKeyEntity {
        public string Key { get; set; } = "";
    }

    private class InheritanceRoot {
        public Guid Id { get; set; }
    }

    private sealed class InheritanceDerived : InheritanceRoot {
        public string? Note { get; set; }
    }
}
