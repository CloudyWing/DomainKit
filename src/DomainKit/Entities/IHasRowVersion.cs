namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得並行控制 token。
/// </summary>
public interface IHasRowVersion {
    /// <summary>
    /// 取得並行控制 token。
    /// </summary>
    byte[] RowVersion { get; }
}
