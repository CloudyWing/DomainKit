using CloudyWing.DomainKit.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;

/// <summary>
/// 在 EF Core 儲存前將目前租戶識別碼寫入新增實體的 shadow property。
/// </summary>
public sealed class TenantStampInterceptor : SaveChangesInterceptor {
    private readonly ICurrentTenant currentTenant;

    /// <summary>
    /// 初始化 <see cref="TenantStampInterceptor" /> 類別的新執行個體。
    /// </summary>
    /// <param name="currentTenant">目前租戶脈絡。</param>
    public TenantStampInterceptor(ICurrentTenant currentTenant) {
        this.currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    ) {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    ) {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? dbContext) {
        if (dbContext is null) {
            return;
        }

        IReadOnlyList<EntityEntry> entries = dbContext.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added
                && entry.Metadata.FindProperty("TenantId") is not null)
            .ToList();
        if (entries.Count == 0) {
            return;
        }

        Guid tenantId = currentTenant.TenantId
            ?? throw new InvalidOperationException("多租戶實體寫入時缺少租戶脈絡。");
        foreach (EntityEntry entry in entries) {
            entry.Property("TenantId").CurrentValue = tenantId;
        }
    }
}
