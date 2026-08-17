namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得對外辨識鍵。
/// </summary>
/// <typeparam name="TKey">對外辨識鍵型別。</typeparam>
public interface IHasExternalKey<out TKey> {
    /// <summary>
    /// 取得對外辨識鍵。
    /// </summary>
    TKey ExternalKey { get; }
}
