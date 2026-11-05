using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;

/// <summary>
/// 提供單一行程內的廣播通道。
/// </summary>
public sealed class InProcessEventBroadcast : IEventBroadcast {
    private static readonly Action<ILogger, string, Exception?> SubscriberFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, nameof(SubscriberFailed)),
        "廣播訂閱者處理通知 {NotificationType} 失敗。"
    );
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Func<object, CancellationToken, Task>>>
        subscriptions = new();
    private readonly ILogger logger;

    /// <summary>
    /// 初始化 <see cref="InProcessEventBroadcast" /> 類別的新執行個體，訂閱者失敗時不記錄日誌。
    /// </summary>
    public InProcessEventBroadcast()
        : this(NullLogger<InProcessEventBroadcast>.Instance) {
    }

    /// <summary>
    /// 以指定 Logger 初始化 <see cref="InProcessEventBroadcast" /> 類別的新執行個體。
    /// </summary>
    /// <param name="logger">記錄訂閱者失敗的 Logger。</param>
    public InProcessEventBroadcast(ILogger<InProcessEventBroadcast> logger) {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync(object notification, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(notification);

        if (!subscriptions.TryGetValue(notification.GetType(), out ConcurrentDictionary<Guid,
            Func<object, CancellationToken, Task>>? handlers)) {
            return;
        }

        foreach (Func<object, CancellationToken, Task> handler in handlers.Values) {
            try {
                await handler(notification, cancellationToken).ConfigureAwait(false);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception exception) {
                // 廣播為 best-effort；單一訂閱者失敗不得阻斷其他訂閱者或發布端，只記錄後繼續。
                SubscriberFailed(logger, notification.GetType().FullName ?? notification.GetType().Name, exception);
            }
        }
    }

    /// <inheritdoc />
    public Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default
    ) where TNotification : class {
        return PublishAsync((object)notification, cancellationToken);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TNotification>(Func<TNotification, CancellationToken, Task> handler)
        where TNotification : class {
        ArgumentNullException.ThrowIfNull(handler);

        Guid subscriptionId = Guid.NewGuid();
        ConcurrentDictionary<Guid, Func<object, CancellationToken, Task>> handlers = subscriptions.GetOrAdd(
            typeof(TNotification),
            static _ => new ConcurrentDictionary<Guid, Func<object, CancellationToken, Task>>()
        );
        handlers[subscriptionId] = (notification, cancellationToken) =>
            handler((TNotification)notification, cancellationToken);

        return new Subscription(() => {
            handlers.TryRemove(subscriptionId, out _);
            if (handlers.IsEmpty) {
                subscriptions.TryRemove(typeof(TNotification), out _);
            }
        });
    }

    private sealed class Subscription : IDisposable {
        private readonly Action unsubscribe;
        private int disposed;

        public Subscription(Action unsubscribe) {
            this.unsubscribe = unsubscribe;
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref disposed, 1) == 0) {
                unsubscribe();
            }
        }
    }
}
