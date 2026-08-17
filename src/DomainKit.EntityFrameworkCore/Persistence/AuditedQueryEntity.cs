namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 提供查詢用的審計投影基底。
/// </summary>
/// <typeparam name="TEntity">原始實體型別。</typeparam>
/// <typeparam name="TUserEntity">使用者實體型別。</typeparam>
public class AuditedQueryEntity<TEntity, TUserEntity>
    where TEntity : class
    where TUserEntity : class {
    /// <summary>
    /// 取得或設定原始實體。
    /// </summary>
    public required TEntity Entity { get; init; }

    /// <summary>
    /// 取得或設定建立者實體。
    /// </summary>
    public TUserEntity? Creator { get; init; }

    /// <summary>
    /// 取得或設定最後更新者實體。
    /// </summary>
    public TUserEntity? Updater { get; init; }
}
