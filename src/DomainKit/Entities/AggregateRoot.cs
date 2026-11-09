using System.Collections.ObjectModel;
using CloudyWing.DomainKit.Events;

namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 提供聚合根的識別碼與雙軌事件收集能力。
/// </summary>
/// <typeparam name="TKey">識別碼型別。</typeparam>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot {
    private readonly List<IDomainEvent> domainEvents = [];
    private readonly List<IIntegrationEvent> integrationEvents = [];
    private ReadOnlyCollection<IDomainEvent>? domainEventsView;
    private ReadOnlyCollection<IIntegrationEvent>? integrationEventsView;

    /// <summary>
    /// 初始化 <see cref="AggregateRoot{TKey}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="id">聚合根識別碼。</param>
    protected AggregateRoot(TKey id)
        : base(id) {
    }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => domainEventsView ??= domainEvents.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<IIntegrationEvent> IntegrationEvents => integrationEventsView ??= integrationEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() {
        domainEvents.Clear();
    }

    /// <inheritdoc />
    public void ClearIntegrationEvents() {
        integrationEvents.Clear();
    }

    /// <summary>
    /// 加入 before-commit 領域事件。
    /// </summary>
    /// <param name="domainEvent">要加入的領域事件。</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) {
        ArgumentNullException.ThrowIfNull(domainEvent);

        domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 加入 after-commit 整合事件。
    /// </summary>
    /// <param name="integrationEvent">要加入的整合事件。</param>
    protected void AddIntegrationEvent(IIntegrationEvent integrationEvent) {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        integrationEvents.Add(integrationEvent);
    }
}
