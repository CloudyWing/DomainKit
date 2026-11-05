namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 表示 Outbox 各狀態的訊息統計。
/// </summary>
public sealed class OutboxStatistics {
    /// <summary>
    /// 取得等待派發的訊息筆數。
    /// </summary>
    public required int PendingCount { get; init; }

    /// <summary>
    /// 取得已由 dispatcher 認領、尚未完成的訊息筆數。
    /// </summary>
    public required int ProcessingCount { get; init; }

    /// <summary>
    /// 取得已完成派發且仍在保留期間內的訊息筆數。
    /// </summary>
    public required int ProcessedCount { get; init; }

    /// <summary>
    /// 取得已超過重試上限的訊息筆數。
    /// </summary>
    public required int DeadLetteredCount { get; init; }

    /// <summary>
    /// 取得最早一筆等待派發訊息的事件發生時間。
    /// </summary>
    /// <value>
    /// 有等待派發的訊息時為其中最早的 <see cref="OutboxMessage.OccurredOn" />；否則為 <see langword="null" />。
    /// </value>
    public DateTimeOffset? OldestPendingOccurredOn { get; init; }
}
