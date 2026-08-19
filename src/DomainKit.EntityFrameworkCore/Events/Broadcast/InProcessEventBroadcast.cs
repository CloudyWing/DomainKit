using System.Collections.Concurrent;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;

/// <summary>
/// 提供單一行程內的廣播通道。
/// </summary>
public sealed class InProcessEventBroadcast : IEventBroadcast {
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Func<object, CancellationToken, Task>>>
        subscriptions = new();

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
            } catch {
                // 廣播為 best-effort；單一訂閱者失敗不得阻斷其他訂閱者或發布端。
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
