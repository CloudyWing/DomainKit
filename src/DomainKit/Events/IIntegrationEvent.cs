namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 標記提交成功後派發的整合事件。
/// </summary>
public interface IIntegrationEvent {
    /// <summary>
    /// 取得事件發生時間。
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
