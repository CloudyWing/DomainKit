using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 提供自動 JOIN 建立者與更新者的審計查詢服務基底。
/// </summary>
/// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
/// <typeparam name="TEntity">原始實體型別。</typeparam>
/// <typeparam name="TUserEntity">使用者實體型別。</typeparam>
public abstract class AuditedQueryService<TDbContext, TEntity, TUserEntity>
    : QueryService<TDbContext, AuditedQueryEntity<TEntity, TUserEntity>>
    where TDbContext : DbContext
    where TEntity : class
    where TUserEntity : class {
    /// <summary>
    /// 初始化 <see cref="AuditedQueryService{TDbContext,TEntity,TUserEntity}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="dbContext">資料庫內容。</param>
    protected AuditedQueryService(TDbContext dbContext)
        : base(dbContext) {
    }

    /// <summary>
    /// 取得使用者查詢來源。
    /// </summary>
    protected abstract IQueryable<TUserEntity> Users { get; }

    /// <summary>
    /// 取得建立者識別碼選擇器。
    /// </summary>
    protected abstract Expression<Func<TEntity, Guid>> CreatorIdSelector { get; }

    /// <summary>
    /// 取得最後更新者識別碼選擇器。
    /// </summary>
    protected virtual Expression<Func<TEntity, Guid?>>? UpdaterIdSelector => null;

    /// <summary>
    /// 取得使用者主鍵選擇器。
    /// </summary>
    protected abstract Expression<Func<TUserEntity, Guid>> UserIdSelector { get; }

    /// <inheritdoc />
    protected override IQueryable<AuditedQueryEntity<TEntity, TUserEntity>> CreateQuery() {
        IQueryable<TEntity> entities = CreateEntityQuery();
        IQueryable<TUserEntity> users = Users.AsNoTracking();
        string creatorIdProperty = GetPropertyName(CreatorIdSelector);
        string userIdProperty = GetPropertyName(UserIdSelector);
        var query = from entity in entities
            join creator in users
                on EF.Property<Guid>(entity, creatorIdProperty)
                equals EF.Property<Guid>(creator, userIdProperty) into creators
            from creator in creators.DefaultIfEmpty()
            select new { entity, creator };

        if (UpdaterIdSelector is null) {
            return query.Select(
                item => new AuditedQueryEntity<TEntity, TUserEntity> {
                    Entity = item.entity,
                    Creator = item.creator,
                    Updater = null
                }
            );
        }

        string updaterIdProperty = GetPropertyName(UpdaterIdSelector);

        return from item in query
            join updater in users
                on EF.Property<Guid?>(item.entity, updaterIdProperty)
                equals (Guid?)EF.Property<Guid>(updater, userIdProperty) into updaters
            from updater in updaters.DefaultIfEmpty()
            select new AuditedQueryEntity<TEntity, TUserEntity> {
                Entity = item.entity,
                Creator = item.creator,
                Updater = updater
            };
    }

    /// <summary>
    /// 建立原始實體的查詢基底。
    /// </summary>
    /// <returns>原始實體查詢基底。</returns>
    protected virtual IQueryable<TEntity> CreateEntityQuery() {
        IQueryable<TEntity> query = DbContext.Set<TEntity>().AsNoTracking();

        return IgnoreSoftDeletedFilter ? query.IgnoreQueryFilters() : query;
    }

    private static string GetPropertyName<TSource, TProperty>(Expression<Func<TSource, TProperty>> selector) {
        return selector.Body switch {
            MemberExpression memberExpression => memberExpression.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member.Name,
            _ => throw new InvalidOperationException("Selector 必須直接對應到屬性存取。")
        };
    }
}
