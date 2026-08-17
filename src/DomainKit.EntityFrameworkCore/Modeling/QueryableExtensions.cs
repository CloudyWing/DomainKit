using CloudyWing.DomainKit.Paging;
using Microsoft.EntityFrameworkCore;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

/// <summary>
/// 提供查詢分頁擴充方法。
/// </summary>
public static class QueryableExtensions {
    /// <summary>
    /// 將查詢轉換為分頁結果。
    /// </summary>
    /// <typeparam name="TItem">項目型別。</typeparam>
    /// <param name="query">查詢來源。</param>
    /// <param name="pageNumber">頁碼。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>分頁結果。</returns>
    public static async Task<PagedList<TItem>> ToPagedListAsync<TItem>(
        this IQueryable<TItem> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<TItem> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        PagingMetadata metadata = new(pageNumber, pageSize, totalCount);

        return new PagedList<TItem>(items, metadata);
    }
}
