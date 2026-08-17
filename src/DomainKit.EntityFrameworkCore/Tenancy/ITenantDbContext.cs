namespace CloudyWing.DomainKit.EntityFrameworkCore.Tenancy;

/// <summary>
/// 提供 EF Core 全域查詢篩選所需的目前租戶參數。
/// </summary>
public interface ITenantDbContext {
    /// <summary>
    /// 取得目前租戶識別碼。
    /// </summary>
    Guid? CurrentTenantId { get; }
}
