namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;

/// <summary>
/// 表示攜帶租戶脈絡與發生時間的跨實例廣播封套。
/// </summary>
/// <typeparam name="TPayload">廣播內容型別。</typeparam>
public sealed class BroadcastEnvelope<TPayload> where TPayload : class {
    /// <summary>
    /// 初始化廣播封套。
    /// </summary>
    public BroadcastEnvelope(Guid? tenantId, TPayload payload, DateTimeOffset occurredOn) {
        ArgumentNullException.ThrowIfNull(payload);
        TenantId = tenantId;
        Payload = payload;
        OccurredOn = occurredOn.ToUniversalTime();
    }

    /// <summary>
    /// 取得租戶識別碼。
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// 取得廣播內容。
    /// </summary>
    public TPayload Payload { get; }

    /// <summary>
    /// 取得廣播發生時間。
    /// </summary>
    public DateTimeOffset OccurredOn { get; }
}
