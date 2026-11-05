using CloudyWing.DomainKit.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;

/// <summary>
/// 在 EF Core 儲存前將目前租戶識別碼寫入新增實體的 shadow property。
/// </summary>
/// <remarks>
/// 新增實體的 <c>TenantId</c> 一律以目前租戶覆寫，呼叫端先行指定的值不會保留。
/// 此為刻意設計，確保寫入的資料只能落在目前租戶範圍，避免應用程式碼誤將資料寫入其他租戶。
/// 需要代替其他租戶寫入時，以 <see cref="ICurrentTenant.Change(Guid?)" /> 切換租戶脈絡。
/// </remarks>
public sealed class TenantStampInterceptor : SaveChangesInterceptor {
    private readonly ICurrentTenant currentTenant;

    /// <summary>
    /// 初始化 <see cref="TenantStampInterceptor" /> 類別的新執行個體。
    /// </summary>
    /// <param name="currentTenant">目前租戶脈絡。</param>
    public TenantStampInterceptor(ICurrentTenant currentTenant) {
        ArgumentNullException.ThrowIfNull(currentTenant);

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
