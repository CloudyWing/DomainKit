namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 提供以 GUID v7 為預設識別碼的實體基底類別。
/// </summary>
public abstract class EntityBase : Entity<Guid> {
    /// <summary>
    /// 初始化 <see cref="EntityBase" /> 類別的新執行個體。
    /// </summary>
    protected EntityBase()
        : base(Guid.CreateVersion7()) {
    }

    /// <summary>
    /// 初始化 <see cref="EntityBase" /> 類別的新執行個體。
    /// </summary>
    /// <param name="id">實體識別碼。</param>
    protected EntityBase(Guid id)
        : base(id) {
    }
}
