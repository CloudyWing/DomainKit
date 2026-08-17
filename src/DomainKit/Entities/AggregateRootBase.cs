namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 提供以 GUID v7 為預設識別碼的聚合根基底類別。
/// </summary>
public abstract class AggregateRootBase : AggregateRoot<Guid> {
    /// <summary>
    /// 初始化 <see cref="AggregateRootBase" /> 類別的新執行個體。
    /// </summary>
    protected AggregateRootBase()
        : base(Guid.CreateVersion7()) {
    }

    /// <summary>
    /// 初始化 <see cref="AggregateRootBase" /> 類別的新執行個體。
    /// </summary>
    /// <param name="id">聚合根識別碼。</param>
    protected AggregateRootBase(Guid id)
        : base(id) {
    }
}
