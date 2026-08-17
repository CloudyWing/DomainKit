using System.Linq.Expressions;
using CloudyWing.DomainKit.Paging;
using CloudyWing.DomainKit.Specifications;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 提供以離散條件建立規約後委派至查詢服務的方法。
/// </summary>
public static class QueryServiceSpecificationExtensions {
    /// <summary>
    /// 以離散條件判斷是否存在符合條件的資料。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="queryService">查詢服務。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若存在符合條件的資料則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    public static Task<bool> AnyAsync<TEntity>(
        this IQueryService<TEntity> queryService,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(predicate);

        return queryService.AnyAsync(new Specification<TEntity>(predicate), cancellationToken);
    }

    /// <summary>
    /// 以離散條件計算符合條件的資料筆數。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <param name="queryService">查詢服務。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>資料筆數。</returns>
    public static Task<int> CountAsync<TEntity>(
        this IQueryService<TEntity> queryService,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(predicate);

        return queryService.CountAsync(new Specification<TEntity>(predicate), cancellationToken);
    }

    /// <summary>
    /// 以離散條件建立規約後取得投影清單。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <typeparam name="TRecord">投影結果型別。</typeparam>
    /// <param name="queryService">查詢服務。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影清單。</returns>
    public static Task<IReadOnlyList<TRecord>> GetListAsync<TEntity, TRecord>(
        this IQueryService<TEntity> queryService,
        Expression<Func<TEntity, TRecord>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return queryService.GetListAsync(selector, new Specification<TEntity>(predicate), orderBy, cancellationToken);
    }

    /// <summary>
    /// 以離散條件建立分頁規約後取得前幾筆投影結果。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <typeparam name="TRecord">投影結果型別。</typeparam>
    /// <param name="queryService">查詢服務。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="take">要取得的筆數。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>投影清單。</returns>
    public static async Task<IReadOnlyList<TRecord>> GetListAsync<TEntity, TRecord>(
        this IQueryService<TEntity> queryService,
        Expression<Func<TEntity, TRecord>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        int take,
        CancellationToken cancellationToken = default
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(orderBy);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        PagedList<TRecord> page = await queryService.GetPagedListAsync(
            selector,
            PagedSpecification<TEntity>.Create(new Specification<TEntity>(predicate), orderBy, 1, take),
            cancellationToken
        ).ConfigureAwait(false);

        return page.Items;
    }

    /// <summary>
    /// 以離散條件建立分頁規約後取得分頁投影結果。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <typeparam name="TRecord">投影結果型別。</typeparam>
    /// <param name="queryService">查詢服務。</param>
    /// <param name="pageNumber">頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分頁投影結果。</returns>
    public static Task<PagedList<TRecord>> GetPagedListAsync<TEntity, TRecord>(
        this IQueryService<TEntity> queryService,
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TRecord>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        CancellationToken cancellationToken = default
    ) where TEntity : class {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(orderBy);

        return queryService.GetPagedListAsync(
            selector,
            PagedSpecification<TEntity>.Create(new Specification<TEntity>(predicate), orderBy, pageNumber, pageSize),
            cancellationToken
        );
    }

    /// <summary>
    /// 以離散條件建立分頁規約後取得分頁投影結果。
    /// </summary>
    /// <typeparam name="TEntity">實體型別。</typeparam>
    /// <typeparam name="TRecord">投影結果型別。</typeparam>
    /// <param name="queryService">查詢服務。</param>
    /// <param name="selector">投影選擇器。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="pageNumber">頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分頁投影結果。</returns>
    public static Task<PagedList<TRecord>> GetPagedListAsync<TEntity, TRecord>(
        this IQueryService<TEntity> queryService,
        Expression<Func<TEntity, TRecord>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    ) where TEntity : class {
        return GetPagedListAsync(queryService, pageNumber, pageSize, selector, predicate, orderBy, cancellationToken);
    }
}
