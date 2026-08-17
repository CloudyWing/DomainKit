using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class TestDbContext : DbContext {
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) {
    }

    public DbSet<AuditedAggregate> AuditedAggregates => Set<AuditedAggregate>();

    public DbSet<ActivityStatusEntity> ActivityStatusEntities => Set<ActivityStatusEntity>();

    public DbSet<AggregateIncludeRoot> AggregateIncludeRoots => Set<AggregateIncludeRoot>();

    public DbSet<CreationAuditedEntity> CreationAuditedEntities => Set<CreationAuditedEntity>();

    public DbSet<HardDeleteEntity> HardDeleteEntities => Set<HardDeleteEntity>();

    public DbSet<ExternalKeyEntity> ExternalKeyEntities => Set<ExternalKeyEntity>();

    public DbSet<SideEffectEntity> SideEffects => Set<SideEffectEntity>();

    public DbSet<TestUserEntity> TestUsers => Set<TestUserEntity>();

    public DbSet<TimestampEntity> TimestampEntities => Set<TimestampEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        configurationBuilder.ApplyDomainKitConventions(new UtcDateTimeConverter())
            .ApplyEnumerationConventions(typeof(TestDbContext).Assembly);
    }

    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder) {
        modelBuilder.ConfigureOutbox();
        modelBuilder.Entity<AuditedAggregate>(
            builder => {
                builder.ConfigureFullAuditedEntity();
                builder.ConfigureRowVersion();
                builder.Property(entity => entity.Name).IsRequired();
                builder.Property(entity => entity.ExternalKey).IsRequired();
            }
        );
        modelBuilder.Entity<ActivityStatusEntity>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Status).IsRequired();
                builder.Property(entity => entity.NullableStatus);
            }
        );
        modelBuilder.Entity<AggregateIncludeRoot>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
                builder.HasMany(entity => entity.Children)
                    .WithOne()
                    .HasForeignKey("AggregateIncludeRootId");
                builder.HasMany(entity => entity.Tags)
                    .WithOne()
                    .HasForeignKey("AggregateIncludeRootId");
                builder.HasAggregateIncludes(includes => includes
                    .Include(
                        entity => entity.Children,
                        child => child
                            .Include(item => item.Options)
                            .Include(item => item.ValidationRules)
                    ));
            }
        );
        modelBuilder.Entity<AggregateIncludeChild>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
                builder.HasMany(entity => entity.Options)
                    .WithOne()
                    .HasForeignKey("AggregateIncludeChildId");
                builder.HasMany(entity => entity.ValidationRules)
                    .WithOne()
                    .HasForeignKey("AggregateIncludeChildId");
            }
        );
        modelBuilder.Entity<AggregateIncludeOption>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
        modelBuilder.Entity<AggregateIncludeRule>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
        modelBuilder.Entity<AggregateIncludeTag>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
        modelBuilder.Entity<CreationAuditedEntity>(
            builder => {
                builder.ConfigureCreationUserAuditedEntity();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
        modelBuilder.Entity<HardDeleteEntity>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
        modelBuilder.Entity<ExternalKeyEntity>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.ExternalKey).IsRequired();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
        modelBuilder.Entity<SideEffectEntity>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Note).IsRequired();
            }
        );
        modelBuilder.Entity<TestUserEntity>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.DisplayName).IsRequired();
            }
        );
        modelBuilder.Entity<TimestampEntity>(
            builder => {
                builder.ConfigureEntityBase();
                builder.Property(entity => entity.Name).IsRequired();
            }
        );
    }
}
