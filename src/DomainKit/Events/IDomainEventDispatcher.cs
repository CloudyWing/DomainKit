namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 派發 before-commit 領域事件。
/// </summary>
public interface IDomainEventDispatcher {
    /// <summary>
    /// 派發領域事件集合。
    /// </summary>
    /// <param name="domainEvents">領域事件集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
