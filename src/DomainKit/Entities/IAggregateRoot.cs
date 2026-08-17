using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 取得聚合根上待派發的領域事件與整合事件。
/// </summary>
public interface IAggregateRoot {
    /// <summary>
    /// 取得 before-commit 領域事件集合。
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// 取得 after-commit 整合事件集合。
    /// </summary>
    IReadOnlyList<IIntegrationEvent> IntegrationEvents { get; }

    /// <summary>
    /// 清除 before-commit 領域事件集合。
    /// </summary>
    void ClearDomainEvents();

    /// <summary>
    /// 清除 after-commit 整合事件集合。
    /// </summary>
    void ClearIntegrationEvents();
}
