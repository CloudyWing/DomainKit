using CloudyWing.DomainKit.Tenancy;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;

/// <summary>
/// 提供跨實例廣播的租戶 envelope 發布與接收 helper。
/// </summary>
public static class EventBroadcastEnvelopeExtensions {
    /// <summary>
    /// 以目前租戶脈絡發布 envelope。
    /// </summary>
    public static Task PublishEnvelopeAsync<TPayload>(
        this IEventBroadcast eventBroadcast,
        TPayload payload,
        ICurrentTenant currentTenant,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default
    ) where TPayload : class {
        ArgumentNullException.ThrowIfNull(eventBroadcast);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(currentTenant);
        ArgumentNullException.ThrowIfNull(timeProvider);
        BroadcastEnvelope<TPayload> envelope = new(
            currentTenant.TenantId,
            payload,
            timeProvider.GetUtcNow()
        );
        return eventBroadcast.PublishAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// 訂閱 envelope，並於處理 payload 前暫時還原其租戶脈絡。
    /// </summary>
    public static IDisposable SubscribeEnvelope<TPayload>(
        this IEventBroadcast eventBroadcast,
        ICurrentTenant currentTenant,
        Func<TPayload, CancellationToken, Task> handler
    ) where TPayload : class {
        ArgumentNullException.ThrowIfNull(eventBroadcast);
        ArgumentNullException.ThrowIfNull(currentTenant);
        ArgumentNullException.ThrowIfNull(handler);
        return eventBroadcast.Subscribe<BroadcastEnvelope<TPayload>>(
            async (envelope, cancellationToken) => {
                using IDisposable tenantScope = currentTenant.Change(envelope.TenantId);
                await handler(envelope.Payload, cancellationToken).ConfigureAwait(false);
            }
        );
    }
}
