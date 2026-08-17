namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得建立者識別碼。
/// </summary>
public interface IHasCreator {
    /// <summary>
    /// 取得建立者識別碼。
    /// </summary>
    Guid CreatorId { get; }
}
