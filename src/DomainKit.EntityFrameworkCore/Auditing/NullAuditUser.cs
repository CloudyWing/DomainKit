namespace CloudyWing.DomainKit.EntityFrameworkCore.Auditing;

/// <summary>
/// 表示未提供審計使用者的預設實作。
/// </summary>
public sealed class NullAuditUser : IAuditUser {
    /// <inheritdoc />
    public Guid? UserId => null;
}
