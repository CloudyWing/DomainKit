namespace CloudyWing.DomainKit.Paging;

/// <summary>
/// 表示以排序鍵定位的 keyset 分頁結果。
/// </summary>
/// <typeparam name="TItem">項目型別。</typeparam>
/// <typeparam name="TKey">排序鍵型別。</typeparam>
public sealed class KeysetPage<TItem, TKey> {
    /// <summary>
    /// 初始化 <see cref="KeysetPage{TItem,TKey}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="items">目前頁資料集合。</param>
    /// <param name="nextKey">取得下一頁時傳入的排序鍵。</param>
    /// <param name="hasMore">若目前頁之後仍有資料則為 <see langword="true" />；否則為 <see langword="false" />。</param>
    public KeysetPage(IReadOnlyList<TItem> items, TKey? nextKey, bool hasMore) {
        ArgumentNullException.ThrowIfNull(items);

        Items = items;
        NextKey = nextKey;
        HasMore = hasMore;
    }

    /// <summary>
    /// 取得目前頁資料集合。
    /// </summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>
    /// 取得目前頁最後一筆資料的排序鍵，供查詢下一頁時傳入。
    /// </summary>
    /// <value>
    /// 目前頁有資料時為最後一筆的排序鍵；目前頁沒有資料時為 <typeparamref name="TKey" /> 的預設值。
    /// </value>
    public TKey? NextKey { get; }

    /// <summary>
    /// 取得一個值，用以指出目前頁之後是否仍有資料。
    /// </summary>
    public bool HasMore { get; }
}
