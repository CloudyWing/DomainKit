using CloudyWing.DomainKit.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events;

/// <summary>
/// 以 DI 容器解析並派發領域事件處理器。
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher {
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// 初始化 <see cref="DomainEventDispatcher" /> 類別的新執行個體。
    /// </summary>
    /// <param name="serviceProvider">服務提供者。</param>
    public DomainEventDispatcher(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (IDomainEvent domainEvent in domainEvents) {
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);
            foreach (object? handler in handlers) {
                if (handler is null) {
                    continue;
                }

                await EventHandlerInvoker.InvokeAsync(
                    handler,
                    domainEvent,
                    typeof(IDomainEventHandler<>),
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }
    }
}
