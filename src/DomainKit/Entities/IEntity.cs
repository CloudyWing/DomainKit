namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得領域實體的識別碼。
/// </summary>
/// <typeparam name="TKey">識別碼型別。</typeparam>
public interface IEntity<out TKey> {
    /// <summary>
    /// 取得實體識別碼。
    /// </summary>
    TKey Id { get; }
}
