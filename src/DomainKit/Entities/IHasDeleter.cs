namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得刪除者識別碼。
/// </summary>
public interface IHasDeleter {
    /// <summary>
    /// 取得刪除者識別碼。
    /// </summary>
    Guid? DeleterId { get; }
}
