using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 提供 Outbox 模型設定擴充方法。
/// </summary>
public static class OutboxModelBuilderExtensions {
    /// <summary>
    /// 設定 Outbox 與事件判重資料表。
    /// </summary>
    /// <param name="modelBuilder">模型建置器。</param>
    /// <returns>模型建置器。</returns>
    public static ModelBuilder ConfigureOutbox(this ModelBuilder modelBuilder) {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(builder => {
            builder.ToTable("OutboxMessages");
            builder.HasKey(message => message.Id);
            builder.Property(message => message.TenantId);
            builder.Property(message => message.EventType).HasMaxLength(512).IsRequired();
            builder.Property(message => message.Payload).IsRequired();
            builder.Property(message => message.TraceParent).HasMaxLength(128);
            builder.Property(message => message.OccurredOn)
                .HasConversion(
                    value => value.UtcTicks,
                    value => new DateTimeOffset(value, TimeSpan.Zero)
                )
                .IsRequired();
            builder.Property(message => message.Status).HasConversion<int>().IsRequired();
            builder.Property(message => message.NextRetryOn)
                .HasConversion(
                    value => value.UtcTicks,
                    value => new DateTimeOffset(value, TimeSpan.Zero)
                )
                .IsRequired();
            builder.Property(message => message.LockedBy).HasMaxLength(200);
            builder.Property(message => message.LockedUntil)
                .HasConversion(
                    value => value.HasValue ? value.Value.UtcTicks : (long?)null,
                    value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null
                );
            builder.Property(message => message.ProcessedOn)
                .HasConversion(
                    value => value.HasValue ? value.Value.UtcTicks : (long?)null,
                    value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null
                );
            builder.Property(message => message.LastError).HasMaxLength(2000);
            builder.HasIndex(message => new { message.TenantId, message.Status, message.NextRetryOn });
            builder.HasIndex(message => message.OccurredOn);
        });
        modelBuilder.Entity<ProcessedEvent>(builder => {
            builder.ToTable("ProcessedEvents");
            builder.HasKey(processed => new { processed.HandlerName, processed.EventId });
            builder.Property(processed => processed.HandlerName).HasMaxLength(512).IsRequired();
            builder.Property(processed => processed.ProcessedOn)
                .HasConversion(
                    value => value.UtcTicks,
                    value => new DateTimeOffset(value, TimeSpan.Zero)
                )
                .IsRequired();
            builder.HasIndex(processed => processed.ProcessedOn);
        });

        return modelBuilder;
    }
}
