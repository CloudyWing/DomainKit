namespace CloudyWing.DomainKit.Paging;

/// <summary>
/// 表示含分頁資訊的唯讀資料集合。
/// </summary>
/// <typeparam name="TItem">項目型別。</typeparam>
public sealed class PagedList<TItem> {
    /// <summary>
    /// 初始化 <see cref="PagedList{TItem}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="items">目前頁資料集合。</param>
    /// <param name="metadata">分頁統計資訊。</param>
    public PagedList(IReadOnlyList<TItem> items, PagingMetadata metadata) {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(metadata);

        Items = items;
        Metadata = metadata;
    }

    /// <summary>
    /// 取得目前頁資料集合。
    /// </summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>
    /// 取得分頁統計資訊。
    /// </summary>
    public PagingMetadata Metadata { get; }
}
