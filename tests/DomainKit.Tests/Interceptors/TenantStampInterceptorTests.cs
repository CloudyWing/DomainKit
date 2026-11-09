using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Interceptors;

[TestFixture]
public sealed class TenantStampInterceptorTests {
    [Test]
    public void Constructor_NullCurrentTenant_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => _ = new TenantStampInterceptor(null!));
    }

    [Test]
    public async Task SaveChangesAsync_ManuallyAssignedTenant_OverwritesWithCurrentTenantAsync() {
        await using StampFixture fixture = await StampFixture.CreateAsync();
        StampDbContext dbContext = fixture.DbContext;
        AmbientCurrentTenant currentTenant = fixture.CurrentTenant;
        Guid currentTenantId = Guid.CreateVersion7();
        StampEntity entity = new() { Name = "Manual" };
        dbContext.Entities.Add(entity);
        dbContext.Entry(entity).Property<Guid>("TenantId").CurrentValue = Guid.CreateVersion7();

        using (currentTenant.Change(currentTenantId)) {
            await dbContext.SaveChangesAsync();
        }

        Assert.That(dbContext.Entry(entity).Property<Guid>("TenantId").CurrentValue, Is.EqualTo(currentTenantId));
    }

    [Test]
    public async Task SaveChanges_SyncPathWithTenant_StampsCurrentTenantAsync() {
        await using StampFixture fixture = await StampFixture.CreateAsync();
        StampDbContext dbContext = fixture.DbContext;
        AmbientCurrentTenant currentTenant = fixture.CurrentTenant;
        Guid currentTenantId = Guid.CreateVersion7();
        StampEntity entity = new() { Name = "Sync" };
        dbContext.Entities.Add(entity);

        using (currentTenant.Change(currentTenantId)) {
            dbContext.SaveChanges();
        }

        Assert.That(dbContext.Entry(entity).Property<Guid>("TenantId").CurrentValue, Is.EqualTo(currentTenantId));
    }

    [Test]
    public async Task SaveChangesAsync_ModifiedEntity_KeepsOriginalTenantAsync() {
        await using StampFixture fixture = await StampFixture.CreateAsync();
        StampDbContext dbContext = fixture.DbContext;
        AmbientCurrentTenant currentTenant = fixture.CurrentTenant;
        Guid originalTenantId = Guid.CreateVersion7();
        StampEntity entity = new() { Name = "Original" };
        dbContext.Entities.Add(entity);
        using (currentTenant.Change(originalTenantId)) {
            await dbContext.SaveChangesAsync();
        }

        entity.Name = "Updated";
        using (currentTenant.Change(Guid.CreateVersion7())) {
            await dbContext.SaveChangesAsync();
        }

        Assert.That(dbContext.Entry(entity).Property<Guid>("TenantId").CurrentValue, Is.EqualTo(originalTenantId));
    }

    [Test]
    public async Task SaveChangesAsync_NoTenantEntityWithoutTenantContext_SavesWithoutExceptionAsync() {
        await using StampFixture fixture = await StampFixture.CreateAsync();
        StampDbContext dbContext = fixture.DbContext;
        dbContext.Untenanted.Add(new UntenantedEntity { Name = "Global" });

        int affected = await dbContext.SaveChangesAsync();

        Assert.That(affected, Is.EqualTo(1));
    }

    private sealed class StampFixture : IAsyncDisposable {
        private readonly SqliteConnection connection;

        private StampFixture(SqliteConnection connection, AmbientCurrentTenant currentTenant, StampDbContext dbContext) {
            this.connection = connection;
            CurrentTenant = currentTenant;
            DbContext = dbContext;
        }

        public AmbientCurrentTenant CurrentTenant { get; }

        public StampDbContext DbContext { get; }

        public static async Task<StampFixture> CreateAsync() {
            SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            AmbientCurrentTenant currentTenant = new();
            DbContextOptions<StampDbContext> options = new DbContextOptionsBuilder<StampDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new TenantStampInterceptor(currentTenant))
                .Options;
            StampDbContext dbContext = new(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new StampFixture(connection, currentTenant, dbContext);
        }

        public async ValueTask DisposeAsync() {
            await DbContext.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class StampDbContext : DbContext {
        public StampDbContext(DbContextOptions<StampDbContext> options)
            : base(options) {
        }

        public DbSet<StampEntity> Entities => Set<StampEntity>();

        public DbSet<UntenantedEntity> Untenanted => Set<UntenantedEntity>();

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.Entity<StampEntity>(builder => {
                builder.HasKey(entity => entity.Id);
                builder.Property<Guid>("TenantId");
            });
            modelBuilder.Entity<UntenantedEntity>(builder => builder.HasKey(entity => entity.Id));
        }
    }

    private sealed class StampEntity {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public required string Name { get; set; }
    }

    private sealed class UntenantedEntity {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public required string Name { get; set; }
    }
}
