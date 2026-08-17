namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;

/// <summary>
/// 定義每個執行實例都可接收的 best-effort 廣播通道。
/// </summary>
public interface IEventBroadcast {
    /// <summary>
    /// 發布廣播通知。
    /// </summary>
    /// <param name="notification">通知。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task PublishAsync(object notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 發布廣播通知。
    /// </summary>
    /// <typeparam name="TNotification">通知型別。</typeparam>
    /// <param name="notification">通知。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default
    ) where TNotification : class;

    /// <summary>
    /// 訂閱廣播通知。
    /// </summary>
    /// <typeparam name="TNotification">通知型別。</typeparam>
    /// <param name="handler">通知處理器。</param>
    /// <returns>用於取消訂閱的資源。</returns>
    IDisposable Subscribe<TNotification>(Func<TNotification, CancellationToken, Task> handler)
        where TNotification : class;
}
