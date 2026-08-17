using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events;

internal static class EventHandlerInvoker {
    private static readonly ConcurrentDictionary<(Type HandlerType, Type EventType),
        Func<object, object, CancellationToken, Task>> Invokers = new();

    public static Task InvokeAsync(
        object handler,
        object eventData,
        Type openHandlerType,
        CancellationToken cancellationToken
    ) {
        Type eventType = eventData.GetType();
        Type handlerType = openHandlerType.MakeGenericType(eventType);
        Func<object, object, CancellationToken, Task> invoker = Invokers.GetOrAdd(
            (handlerType, eventType),
            static key => BuildInvoker(key.HandlerType, key.EventType)
        );

        return invoker(handler, eventData, cancellationToken);
    }

    private static Func<object, object, CancellationToken, Task> BuildInvoker(Type handlerType, Type eventType) {
        MethodInfo method = handlerType.GetMethod("HandleAsync", [eventType, typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"找不到 {handlerType.FullName}.HandleAsync 方法。");
        ParameterExpression handler = Expression.Parameter(typeof(object), "handler");
        ParameterExpression eventData = Expression.Parameter(typeof(object), "eventData");
        ParameterExpression cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
        MethodCallExpression call = Expression.Call(
            Expression.Convert(handler, handlerType),
            method,
            Expression.Convert(eventData, eventType),
            cancellationToken
        );

        return Expression.Lambda<Func<object, object, CancellationToken, Task>>(
            call,
            handler,
            eventData,
            cancellationToken
        ).Compile();
    }
}
