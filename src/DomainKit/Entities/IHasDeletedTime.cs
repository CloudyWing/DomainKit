namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得軟刪除時間。
/// </summary>
public interface IHasDeletedTime {
    /// <summary>
     /// 取得刪除時間。
     /// </summary>
    DateTimeOffset? DeletedTime { get; }
}
