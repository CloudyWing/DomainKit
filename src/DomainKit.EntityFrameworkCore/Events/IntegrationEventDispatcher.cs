using System.Collections.Concurrent;
using CloudyWing.DomainKit.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events;

/// <summary>
/// 以 DI 容器解析並派發整合事件處理器。
/// </summary>
public sealed class IntegrationEventDispatcher : IIntegrationEventDispatcher {
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypes = new();
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// 初始化 <see cref="IntegrationEventDispatcher" /> 類別的新執行個體。
    /// </summary>
    /// <param name="serviceProvider">服務提供者。</param>
    public IntegrationEventDispatcher(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(integrationEvents);

        foreach (IIntegrationEvent integrationEvent in integrationEvents) {
            Type handlerType = HandlerTypes.GetOrAdd(
                integrationEvent.GetType(),
                static eventType => typeof(IIntegrationEventHandler<>).MakeGenericType(eventType)
            );
            IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);
            foreach (object? handler in handlers) {
                if (handler is null) {
                    continue;
                }

                await EventHandlerInvoker.InvokeAsync(
                    handler,
                    integrationEvent,
                    typeof(IIntegrationEventHandler<>),
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }
    }
}
