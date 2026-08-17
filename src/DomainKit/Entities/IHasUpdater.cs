namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得最後更新者識別碼。
/// </summary>
public interface IHasUpdater {
    /// <summary>
    /// 取得最後更新者識別碼。
    /// </summary>
    Guid UpdaterId { get; }
}
