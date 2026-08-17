using System.Linq.Expressions;

namespace CloudyWing.DomainKit.Specifications;

/// <summary>
/// 表示包含排序與分頁資訊的不可變查詢規約。
/// </summary>
/// <typeparam name="T">規約目標型別。</typeparam>
public sealed class PagedSpecification<T> : Specification<T>, IPagedSpecification<T> {
    private PagedSpecification(
        Expression<Func<T, bool>> criteria,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        int? pageNumber,
        int? pageSize
    )
        : base(criteria) {
        ArgumentNullException.ThrowIfNull(orderBy);

        ValidatePagination(pageNumber, pageSize);

        OrderBy = orderBy;
        HasPagination = pageNumber.HasValue;
        PageNumber = pageNumber ?? 0;
        PageSize = pageSize ?? 0;
    }

    /// <summary>
    /// 取得排序函式。
    /// </summary>
    public Func<IQueryable<T>, IOrderedQueryable<T>> OrderBy { get; }

    /// <summary>
    /// 取得頁碼。
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// 取得每頁筆數。
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// 取得一個值，用以指出是否包含分頁資訊。
    /// </summary>
    public bool HasPagination { get; }

    /// <summary>
    /// 以既有規約與排序建立排序規約。
    /// </summary>
    /// <param name="specification">基礎規約。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <returns>排序規約。</returns>
    public static PagedSpecification<T> Create(
        ISpecification<T> specification,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy
    ) {
        ArgumentNullException.ThrowIfNull(specification);

        return new PagedSpecification<T>(specification.ToExpression(), orderBy, null, null);
    }

    /// <summary>
    /// 以既有規約、排序與分頁資訊建立分頁規約。
    /// </summary>
    /// <param name="specification">基礎規約。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="pageNumber">頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <returns>分頁規約。</returns>
    public static PagedSpecification<T> Create(
        ISpecification<T> specification,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        int pageNumber,
        int pageSize
    ) {
        ArgumentNullException.ThrowIfNull(specification);

        return new PagedSpecification<T>(specification.ToExpression(), orderBy, pageNumber, pageSize);
    }

    /// <summary>
    /// 以條件運算式與排序建立排序規約。
    /// </summary>
    /// <param name="criteria">篩選條件運算式。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <returns>排序規約。</returns>
    public static PagedSpecification<T> Create(
        Expression<Func<T, bool>> criteria,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy
    ) {
        return new PagedSpecification<T>(criteria, orderBy, null, null);
    }

    /// <summary>
    /// 以條件運算式、排序與分頁資訊建立分頁規約。
    /// </summary>
    /// <param name="criteria">篩選條件運算式。</param>
    /// <param name="orderBy">排序函式。</param>
    /// <param name="pageNumber">頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <returns>分頁規約。</returns>
    public static PagedSpecification<T> Create(
        Expression<Func<T, bool>> criteria,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        int pageNumber,
        int pageSize
    ) {
        return new PagedSpecification<T>(criteria, orderBy, pageNumber, pageSize);
    }

    private static void ValidatePagination(int? pageNumber, int? pageSize) {
        if (pageNumber.HasValue != pageSize.HasValue) {
            throw new ArgumentException("Page number and page size must both be provided.");
        }

        if (!pageNumber.HasValue || !pageSize.HasValue) {
            return;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber.Value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize.Value);
    }
}
