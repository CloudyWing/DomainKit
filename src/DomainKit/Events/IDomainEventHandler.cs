namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 處理指定領域事件型別。
/// </summary>
/// <typeparam name="TEvent">領域事件型別。</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent {
    /// <summary>
    /// 處理領域事件。
    /// </summary>
    /// <param name="domainEvent">領域事件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
