namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 派發 after-commit 整合事件。
/// </summary>
public interface IIntegrationEventDispatcher {
    /// <summary>
    /// 派發整合事件集合。
    /// </summary>
    /// <param name="integrationEvents">整合事件集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task DispatchAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken = default);
}
