using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.EntityFrameworkCore.Tenancy;
using CloudyWing.DomainKit.Tenancy;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class NamedQueryFilterTests {
    [Test]
    public async Task UseDomainKitNamedQueryFilters_FullAuditedTenantEntity_RegistersNamedFiltersAsync() {
        await using NamedFilterFixture fixture = await NamedFilterFixture.CreateAsync();

        IEntityType entityType = fixture.DbContext.Model.FindEntityType(typeof(AuditedAggregate))!;
        IEnumerable<string?> keys = entityType.GetDeclaredQueryFilters().Select(filter => filter.Key);

        Assert.That(keys, Is.EquivalentTo(new[] { DomainKitQueryFilterNames.SoftDelete, DomainKitQueryFilterNames.Tenant }));
    }

    [Test]
    public async Task CreateQuery_DefaultSwitches_FiltersDeletedAndOtherTenantRowsAsync() {
        await using NamedFilterFixture fixture = await NamedFilterFixture.CreateAsync();
        NamedFilterQueryService sut = new(fixture.DbContext);

        IReadOnlyList<string> actual = await fixture.QueryNamesAsCurrentTenantAsync(sut);

        Assert.That(actual, Is.EqualTo(new[] { "Current-Active" }));
    }

    [Test]
    public async Task CreateQuery_IgnoreSoftDeleteFilterOnly_IncludesDeletedRowsOfCurrentTenantOnlyAsync() {
        await using NamedFilterFixture fixture = await NamedFilterFixture.CreateAsync();
        NamedFilterQueryService sut = new(fixture.DbContext) {
            IgnoreSoftDeleteFilterOnly = true
        };

        IReadOnlyList<string> actual = await fixture.QueryNamesAsCurrentTenantAsync(sut);

        Assert.That(actual, Is.EqualTo(new[] { "Current-Active", "Current-Deleted" }));
    }

    [Test]
    public async Task CreateQuery_IgnoreSoftDeletedFilter_IgnoresAllNamedFiltersLikeBeforeAsync() {
        await using NamedFilterFixture fixture = await NamedFilterFixture.CreateAsync();
        NamedFilterQueryService sut = new(fixture.DbContext) {
            IgnoreSoftDeletedFilter = true,
            IgnoreSoftDeleteFilterOnly = true
        };

        IReadOnlyList<string> actual = await fixture.QueryNamesAsCurrentTenantAsync(sut);

        Assert.That(actual, Is.EqualTo(new[] { "Current-Active", "Current-Deleted", "Other-Active" }));
    }

    [Test]
    public async Task IgnoreQueryFilters_NamedFilters_ReturnsSameRowsAsAnonymousFiltersAsync() {
        await using NamedFilterFixture fixture = await NamedFilterFixture.CreateAsync();

        List<string> actual = await fixture.DbContext.Aggregates
            .IgnoreQueryFilters()
            .Select(entity => entity.Name)
            .OrderBy(name => name)
            .ToListAsync();

        Assert.That(actual, Is.EqualTo(new[] { "Current-Active", "Current-Deleted", "Other-Active" }));
    }

    [Test]
    public async Task CreateQuery_IgnoreSoftDeleteFilterOnlyWithAnonymousFilter_ThrowsInvalidOperationExceptionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        AnonymousFilterQueryService sut = new(database.DbContext) {
            IgnoreSoftDeleteFilterOnly = true
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.CountAsync());
    }

    [Test]
    public async Task CreateQuery_IgnoreSoftDeleteFilterOnlyWithoutFilters_ReturnsRowsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.DbContext.HardDeleteEntities.Add(new HardDeleteEntity { Name = "Plain" });
        await database.DbContext.SaveChangesAsync();
        HardDeleteQueryService sut = new(database.DbContext) {
            IgnoreSoftDeleteFilterOnly = true
        };

        int actual = await sut.CountAsync();

        Assert.That(actual, Is.EqualTo(1));
    }

    [Test]
    public async Task ConfigureFullAuditedEntity_WithoutOptIn_KeepsAnonymousFilterAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        IEntityType entityType = database.DbContext.Model.FindEntityType(typeof(AuditedAggregate))!;
        IQueryFilter filter = entityType.GetDeclaredQueryFilters().Single();

        Assert.That(filter.IsAnonymous, Is.True);
    }

    private sealed class NamedFilterFixture : IAsyncDisposable {
        private readonly SqliteConnection connection;
        private readonly AmbientCurrentTenant currentTenant;
        private readonly Guid currentTenantId = Guid.CreateVersion7();

        private NamedFilterFixture(SqliteConnection connection, AmbientCurrentTenant currentTenant, NamedFilterDbContext dbContext) {
            this.connection = connection;
            this.currentTenant = currentTenant;
            DbContext = dbContext;
        }

        public NamedFilterDbContext DbContext { get; }

        public static async Task<NamedFilterFixture> CreateAsync() {
            SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            AmbientCurrentTenant currentTenant = new();
            DbContextOptions<NamedFilterDbContext> options = new DbContextOptionsBuilder<NamedFilterDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new TenantStampInterceptor(currentTenant))
                .Options;
            NamedFilterDbContext dbContext = new(options, currentTenant);
            await dbContext.Database.EnsureCreatedAsync();
            NamedFilterFixture fixture = new(connection, currentTenant, dbContext);
            await fixture.SeedAsync();

            return fixture;
        }

        public async Task<IReadOnlyList<string>> QueryNamesAsCurrentTenantAsync(NamedFilterQueryService queryService) {
            using IDisposable scope = currentTenant.Change(currentTenantId);
            IReadOnlyList<string> names = await queryService.GetListAsync(
                entity => entity.Name,
                orderBy: query => query.OrderBy(entity => entity.Name)
            );

            return names;
        }

        public async ValueTask DisposeAsync() {
            await DbContext.DisposeAsync();
            await connection.DisposeAsync();
        }

        private async Task SeedAsync() {
            using (currentTenant.Change(currentTenantId)) {
                DbContext.Aggregates.Add(CreateAggregate("Current-Active"));
                AuditedAggregate deleted = CreateAggregate("Current-Deleted");
                DbContext.Aggregates.Add(deleted);
                DbContext.Entry(deleted).Property(nameof(AuditedAggregate.DeletedTime)).CurrentValue = DateTimeOffset.UtcNow;
                await DbContext.SaveChangesAsync();
            }

            using (currentTenant.Change(Guid.CreateVersion7())) {
                DbContext.Aggregates.Add(CreateAggregate("Other-Active"));
                await DbContext.SaveChangesAsync();
            }

            DbContext.ChangeTracker.Clear();
        }

        private static AuditedAggregate CreateAggregate(string name) {
            return new AuditedAggregate {
                Name = name,
                ExternalKey = Guid.CreateVersion7()
            };
        }
    }

    private sealed class NamedFilterDbContext : DbContext, ITenantDbContext {
        private readonly ICurrentTenant currentTenant;

        public NamedFilterDbContext(DbContextOptions<NamedFilterDbContext> options, ICurrentTenant currentTenant)
            : base(options) {
            this.currentTenant = currentTenant;
        }

        public DbSet<AuditedAggregate> Aggregates => Set<AuditedAggregate>();

        public Guid? CurrentTenantId => currentTenant.TenantId;

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
            modelBuilder.UseDomainKitNamedQueryFilters();
            modelBuilder.Entity<AuditedAggregate>(builder => {
                builder.ConfigureFullAuditedEntity();
                builder.Property(entity => entity.Name).IsRequired();
            });
            modelBuilder.ApplyTenantConventions(this);
        }
    }

    private sealed class NamedFilterQueryService : QueryService<NamedFilterDbContext, AuditedAggregate> {
        public NamedFilterQueryService(NamedFilterDbContext dbContext)
            : base(dbContext) {
        }
    }

    private sealed class AnonymousFilterQueryService : QueryService<TestDbContext, AuditedAggregate> {
        public AnonymousFilterQueryService(TestDbContext dbContext)
            : base(dbContext) {
        }
    }

    private sealed class HardDeleteQueryService : QueryService<TestDbContext, HardDeleteEntity> {
        public HardDeleteQueryService(TestDbContext dbContext)
            : base(dbContext) {
        }
    }
}
