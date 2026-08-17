namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 表示事件處理器的冪等判重紀錄。
/// </summary>
public class ProcessedEvent {
    /// <summary>
    /// 取得處理器穩定識別碼。
    /// </summary>
    /// <remarks>
    /// 值為處理器型別身份的 SHA-256 雜湊，用於控制 SQL Server 複合主鍵的長度。
    /// </remarks>
    public required string HandlerName { get; init; }
    /// <summary>
    /// 取得事件識別碼。
    /// </summary>
    public Guid EventId { get; init; }
    /// <summary>
    /// 取得完成時間。
    /// </summary>
    public DateTimeOffset ProcessedOn { get; init; }
}
