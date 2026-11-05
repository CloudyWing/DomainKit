using CloudyWing.DomainKit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;

/// <summary>
/// 在 EF Core 儲存前為 app-managed 並行控制 token 統一更新版本值。
/// </summary>
public sealed class RowVersionStampInterceptor : SaveChangesInterceptor {
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
        if (eventData.Context is not null) {
            StampRowVersions(eventData.Context);
        }

        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    ) {
        if (eventData.Context is not null) {
            StampRowVersions(eventData.Context);
        }

        return ValueTask.FromResult(result);
    }

    private static void StampRowVersions(DbContext dbContext) {
        foreach (EntityEntry<IHasRowVersion> entry in dbContext.ChangeTracker.Entries<IHasRowVersion>()) {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) {
                continue;
            }

            PropertyEntry rowVersionProperty = entry.Property(nameof(IHasRowVersion.RowVersion));
            if (ShouldSkipStamp(rowVersionProperty)) {
                continue;
            }

            rowVersionProperty.CurrentValue = Guid.CreateVersion7().ToByteArray();
        }
    }

    internal static bool ShouldSkipStamp(PropertyEntry rowVersionProperty) {
        return rowVersionProperty.Metadata.ValueGenerated == ValueGenerated.OnAddOrUpdate;
    }
}
