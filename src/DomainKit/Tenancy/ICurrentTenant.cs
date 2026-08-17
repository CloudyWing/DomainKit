namespace CloudyWing.DomainKit.Tenancy;

/// <summary>
/// 提供目前執行脈絡的租戶識別碼與暫時切換能力。
/// </summary>
public interface ICurrentTenant {
    /// <summary>
    /// 取得目前租戶識別碼。
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// 暫時切換目前租戶，並於回傳的 scope 釋放時還原原租戶。
    /// </summary>
    /// <param name="tenantId">要切換的租戶識別碼；無租戶脈絡時為 <see langword="null" />。</param>
    /// <returns>用於還原原租戶脈絡的 scope。</returns>
    IDisposable Change(Guid? tenantId);
}
