namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 指定 Outbox 訊息狀態。
/// </summary>
public enum OutboxMessageStatus {
    /// <summary>
    /// 等待派發。
    /// </summary>
    Pending,
    /// <summary>
    /// 已由 dispatcher 認領。
    /// </summary>
    Processing,
    /// <summary>
    /// 已完成派發。
    /// </summary>
    Processed,
    /// <summary>
    /// 已超過重試上限。
    /// </summary>
    DeadLettered
}
