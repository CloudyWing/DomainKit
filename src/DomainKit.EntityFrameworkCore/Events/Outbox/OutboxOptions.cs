namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 表示 Outbox dispatcher 選項。
/// </summary>
public sealed class OutboxOptions {
    /// <summary>
    /// 取得或設定無資料時的輪詢間隔。
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);
    /// <summary>
    /// 取得或設定每批最多認領筆數。
    /// </summary>
    public int BatchSize { get; set; } = 50;
    /// <summary>
    /// 取得或設定認領租約時間。
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>
    /// 取得或設定基礎重試延遲。
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>
    /// 取得或設定最大重試延遲。
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(15);
    /// <summary>
    /// 取得或設定最大重試次數。
    /// </summary>
    public int MaxRetryCount { get; set; } = 8;
    /// <summary>
    /// 取得或設定已處理資料保留期間。
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
}
