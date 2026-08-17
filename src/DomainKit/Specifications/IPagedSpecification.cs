namespace CloudyWing.DomainKit.Specifications;

/// <summary>
/// 定義包含排序與分頁資訊的查詢規約。
/// </summary>
/// <typeparam name="T">規約目標型別。</typeparam>
public interface IPagedSpecification<T> : ISpecification<T> {
    /// <summary>
    /// 取得排序函式。
    /// </summary>
    Func<IQueryable<T>, IOrderedQueryable<T>> OrderBy { get; }

    /// <summary>
    /// 取得頁碼。
    /// </summary>
    int PageNumber { get; }

    /// <summary>
    /// 取得每頁筆數。
    /// </summary>
    int PageSize { get; }

    /// <summary>
    /// 取得一個值，用以指出是否包含分頁資訊。
    /// </summary>
    bool HasPagination { get; }
}
