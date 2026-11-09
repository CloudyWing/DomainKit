using System.Linq.Expressions;
using System.Reflection;
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
    /// <remarks>
    /// 呼叫前須以唯一且穩定的鍵排序；未排序或排序鍵不唯一時，資料庫不保證各頁的資料順序，可能出現跨頁重複或遺漏。
    /// 頁碼換算的略過筆數超過 <see cref="int.MaxValue" /> 時以 <see cref="int.MaxValue" /> 計算，回傳空白頁。
    /// </remarks>
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
            .Skip(PagingUtils.GetSkipCount(pageNumber, pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        PagingMetadata metadata = new(pageNumber, pageSize, totalCount);

        return new PagedList<TItem>(items, metadata);
    }

    /// <summary>
    /// 依排序鍵遞增取得 keyset 分頁的第一頁。
    /// </summary>
    /// <typeparam name="TItem">項目型別。</typeparam>
    /// <typeparam name="TKey">排序鍵型別。</typeparam>
    /// <param name="query">查詢來源。</param>
    /// <param name="keySelector">排序鍵選擇器，須指向唯一且不可變的欄位。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>keyset 分頁結果。</returns>
    /// <remarks>
    /// 本方法以 <paramref name="keySelector" /> 遞增排序並取代來源查詢既有的排序。
    /// 取得下一頁時，將回傳結果的 <see cref="KeysetPage{TItem,TKey}.NextKey" /> 傳入接受 <c>afterKey</c> 的多載。
    /// </remarks>
    public static Task<KeysetPage<TItem, TKey>> ToKeysetPageAsync<TItem, TKey>(
        this IQueryable<TItem> query,
        Expression<Func<TItem, TKey>> keySelector,
        int pageSize,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return ToKeysetPageCoreAsync(query, keySelector, pageSize, cancellationToken);
    }

    /// <summary>
    /// 依排序鍵遞增取得指定排序鍵之後的 keyset 分頁。
    /// </summary>
    /// <typeparam name="TItem">項目型別。</typeparam>
    /// <typeparam name="TKey">排序鍵型別。</typeparam>
    /// <param name="query">查詢來源。</param>
    /// <param name="keySelector">排序鍵選擇器，須指向唯一且不可變的欄位。</param>
    /// <param name="afterKey">上一頁的 <see cref="KeysetPage{TItem,TKey}.NextKey" />；為 <see langword="null" /> 時從第一筆開始。</param>
    /// <param name="pageSize">每頁筆數。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>keyset 分頁結果。</returns>
    /// <remarks>
    /// 本方法以 <paramref name="keySelector" /> 遞增排序並取代來源查詢既有的排序，只回傳排序鍵大於 <paramref name="afterKey" /> 的資料。
    /// 數值、日期時間等具比較運算子的型別直接比較；字串以 <see cref="string.Compare(string, string)" /> 比較；
    /// 其他型別以 <c>CompareTo</c> 比較，能否轉譯為 SQL 取決於資料庫 provider。
    /// 實值型別的排序鍵無法以 <see langword="null" /> 表示第一頁，第一頁請改用不含 <paramref name="afterKey" /> 的多載。
    /// </remarks>
    public static Task<KeysetPage<TItem, TKey>> ToKeysetPageAsync<TItem, TKey>(
        this IQueryable<TItem> query,
        Expression<Func<TItem, TKey>> keySelector,
        TKey? afterKey,
        int pageSize,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        IQueryable<TItem> filteredQuery = afterKey is null
            ? query
            : query.Where(CreateAfterKeyPredicate(keySelector, afterKey));

        return ToKeysetPageCoreAsync(filteredQuery, keySelector, pageSize, cancellationToken);
    }

    private static async Task<KeysetPage<TItem, TKey>> ToKeysetPageCoreAsync<TItem, TKey>(
        IQueryable<TItem> query,
        Expression<Func<TItem, TKey>> keySelector,
        int pageSize,
        CancellationToken cancellationToken
    ) {
        // 多取一筆判斷是否還有下一頁，不另外執行 CountAsync。
        int takeCount = pageSize == int.MaxValue ? pageSize : pageSize + 1;
        List<TItem> items = await query
            .OrderBy(keySelector)
            .Take(takeCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        bool hasMore = items.Count > pageSize;
        if (hasMore) {
            items.RemoveAt(items.Count - 1);
        }

        TKey? nextKey = items.Count == 0
            ? default
            : keySelector.Compile(preferInterpretation: true)(items[^1]);

        return new KeysetPage<TItem, TKey>(items, nextKey, hasMore);
    }

    private static Expression<Func<TItem, bool>> CreateAfterKeyPredicate<TItem, TKey>(
        Expression<Func<TItem, TKey>> keySelector,
        TKey afterKey
    ) {
        // 以 closure 捕捉游標值，讓 EF Core 產生 SQL 參數。
        Expression<Func<TKey>> afterKeyValue = () => afterKey;
        Expression comparison = CreateGreaterThan(keySelector.Body, afterKeyValue.Body);

        return Expression.Lambda<Func<TItem, bool>>(comparison, keySelector.Parameters);
    }

    private static Expression CreateGreaterThan(Expression left, Expression right) {
        Type type = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
        if ((type.IsPrimitive && type != typeof(bool))
            || type.GetMethod("op_GreaterThan", [type, type]) is not null
        ) {
            return Expression.GreaterThan(left, right);
        }

        if (type == typeof(string)) {
            MethodInfo compare = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])
                ?? throw new InvalidOperationException("找不到 string.Compare(string, string) 方法。");

            return Expression.GreaterThan(Expression.Call(compare, left, right), Expression.Constant(0));
        }

        MethodInfo compareTo = left.Type.GetMethod(nameof(IComparable.CompareTo), [left.Type])
            ?? throw new NotSupportedException($"排序鍵型別 {left.Type.FullName} 未提供可比較的運算子或 CompareTo 方法。");

        return Expression.GreaterThan(Expression.Call(left, compareTo, right), Expression.Constant(0));
    }
}
