namespace CloudyWing.DomainKit.EntityFrameworkCore.Auditing;

/// <summary>
/// 提供目前審計使用者識別碼。
/// </summary>
public interface IAuditUser {
    /// <summary>
    /// 取得目前使用者識別碼。
    /// </summary>
    Guid? UserId { get; }
}
