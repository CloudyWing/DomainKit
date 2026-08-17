namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得顯示排序值。
/// </summary>
public interface IHasDisplayOrder {
    /// <summary>
    /// 取得顯示排序值。
    /// </summary>
    int DisplayOrder { get; }
}
