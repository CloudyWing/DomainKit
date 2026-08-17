namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得最後更新時間。
/// </summary>
public interface IHasUpdatedTime {
    /// <summary>
    /// 取得最後更新時間。
    /// </summary>
    DateTimeOffset UpdatedTime { get; }
}
