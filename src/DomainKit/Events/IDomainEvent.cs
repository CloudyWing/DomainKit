namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 標記提交前派發且可參與同一交易的領域事件。
/// </summary>
public interface IDomainEvent {
    /// <summary>
    /// 取得事件發生時間。
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
