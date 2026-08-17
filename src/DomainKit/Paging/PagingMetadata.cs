namespace CloudyWing.DomainKit.Paging;

/// <summary>
/// 表示分頁結果的統計資訊。
/// </summary>
public sealed class PagingMetadata {
    /// <summary>
    /// 初始化 <see cref="PagingMetadata" /> 類別的新執行個體。
    /// </summary>
    /// <param name="pageNumber">目前頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="totalCount">總筆數。</param>
    public PagingMetadata(int pageNumber, int pageSize, int totalCount) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    /// <summary>
    /// 取得目前頁碼。
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// 取得每頁筆數。
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// 取得總筆數。
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// 取得總頁數。
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// 取得一個值，用以指出是否有上一頁。
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1 && TotalPages > 0;

    /// <summary>
    /// 取得一個值，用以指出是否有下一頁。
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
