namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 表示待派發的持久化整合事件。
/// </summary>
public class OutboxMessage {
    /// <summary>
    /// 取得事件識別碼。
    /// </summary>
    public Guid Id { get; init; }
    /// <summary>
    /// 取得事件所屬租戶識別碼。
    /// </summary>
    public Guid? TenantId { get; init; }
    /// <summary>
    /// 取得事件 CLR 型別名稱。
    /// </summary>
    public required string EventType { get; init; }
    /// <summary>
    /// 取得序列化事件內容。
    /// </summary>
    public required string Payload { get; init; }
    /// <summary>
    /// 取得產生事件時的 W3C trace parent。
    /// </summary>
    public string? TraceParent { get; init; }
    /// <summary>
    /// 取得事件發生時間。
    /// </summary>
    public DateTimeOffset OccurredOn { get; init; }
    /// <summary>
    /// 取得或設定訊息狀態。
    /// </summary>
    public OutboxMessageStatus Status { get; set; }
    /// <summary>
    /// 取得或設定重試次數。
    /// </summary>
    public int RetryCount { get; set; }
    /// <summary>
    /// 取得或設定下次可派發時間。
    /// </summary>
    public DateTimeOffset NextRetryOn { get; set; }
    /// <summary>
    /// 取得或設定認領者。
    /// </summary>
    public string? LockedBy { get; set; }
    /// <summary>
    /// 取得或設定認領租約到期時間。
    /// </summary>
    public DateTimeOffset? LockedUntil { get; set; }
    /// <summary>
    /// 取得或設定完成時間。
    /// </summary>
    public DateTimeOffset? ProcessedOn { get; set; }
    /// <summary>
    /// 取得或設定最近一次失敗摘要。
    /// </summary>
    public string? LastError { get; set; }
}
