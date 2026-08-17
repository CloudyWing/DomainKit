namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得建立時間。
/// </summary>
public interface IHasCreatedTime {
    /// <summary>
    /// 取得建立時間。
    /// </summary>
    DateTimeOffset CreatedTime { get; }
}
