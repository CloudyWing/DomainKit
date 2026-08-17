namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 處理指定整合事件型別。
/// </summary>
/// <typeparam name="TEvent">整合事件型別。</typeparam>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent {
    /// <summary>
    /// 處理整合事件。
    /// </summary>
    /// <param name="integrationEvent">整合事件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
