using System.Collections.Concurrent;

namespace CloudyWing.DomainKit.Entities;

/// <summary>
/// 提供以識別碼為基礎的實體相等性語意。
/// </summary>
/// <typeparam name="TKey">識別碼型別。</typeparam>
public abstract class Entity<TKey> : IEntity<TKey> {
    private static readonly ConcurrentDictionary<Type, Type> UnproxiedTypes = new();

    /// <summary>
    /// 初始化 <see cref="Entity{TKey}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="id">實體識別碼。</param>
    protected Entity(TKey id) {
        ArgumentNullException.ThrowIfNull(id);

        Id = id;
    }

    /// <inheritdoc />
    public TKey Id { get; protected set; }

    /// <summary>
    /// 判斷目前實體是否與指定物件相等。
    /// </summary>
    /// <param name="obj">要比較的物件。</param>
    /// <returns>若兩者代表同型別且同識別碼的持久化實體則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) {
            return true;
        }

        if (obj is not Entity<TKey> other) {
            return false;
        }

        if (GetUnproxiedType(this) != GetUnproxiedType(other)) {
            return false;
        }

        if (IsTransient() || other.IsTransient()) {
            return false;
        }

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// 取得目前實體的雜湊碼。
    /// </summary>
    /// <returns>依型別與識別碼計算的雜湊碼。</returns>
    public override int GetHashCode() {
        return IsTransient()
            ? base.GetHashCode()
            : HashCode.Combine(GetUnproxiedType(this), Id);
    }

    /// <summary>
    /// 判斷目前實體是否尚未取得持久化識別碼。
    /// </summary>
    /// <returns>若識別碼仍為預設值則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    protected virtual bool IsTransient() {
        return EqualityComparer<TKey>.Default.Equals(Id, default);
    }

    private static Type GetUnproxiedType(object entity) {
        return UnproxiedTypes.GetOrAdd(entity.GetType(), static type => ResolveUnproxiedType(type));
    }

    private static Type ResolveUnproxiedType(Type type) {
        string typeName = type.ToString();

        return typeName.Contains("Castle.Proxies.", StringComparison.Ordinal)
            ? type.BaseType ?? type
            : type;
    }
}
