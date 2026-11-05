using System.Reflection;
using CloudyWing.DomainKit.EntityFrameworkCore.Auditing;
using CloudyWing.DomainKit.EntityFrameworkCore.Events;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using CloudyWing.DomainKit.EntityFrameworkCore.Interceptors;
using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CloudyWing.DomainKit.EntityFrameworkCore.DependencyInjection;

/// <summary>
/// 提供 DomainKit 服務註冊擴充方法。
/// </summary>
public static class DomainKitServiceCollectionExtensions {
    /// <summary>
    /// 註冊 DomainKit EF Core 基礎服務。
    /// </summary>
    /// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
    /// <param name="services">服務集合。</param>
    /// <param name="configure">選項設定函式。</param>
    /// <param name="handlerAssemblies">要掃描事件處理器的組件集合。</param>
    /// <returns>服務集合。</returns>
    public static IServiceCollection AddDomainKit<TDbContext>(
        this IServiceCollection services,
        Action<DomainKitOptions>? configure = null,
        params Assembly[] handlerAssemblies
    ) where TDbContext : DbContext {
        ArgumentNullException.ThrowIfNull(services);

        DomainKitOptions options = new();
        configure?.Invoke(options);

        foreach (Assembly handlerAssembly in handlerAssemblies) {
            options.HandlerAssemblies.Add(handlerAssembly);
        }

        services.TryAddScoped<IAuditUser, NullAuditUser>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.TryAddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.TryAddScoped<IntegrationEventContext>();
        services.TryAddScoped<IIntegrationEventContext>(
            serviceProvider => serviceProvider.GetRequiredService<IntegrationEventContext>()
        );
        services.TryAddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();
        services.TryAddScoped<RowVersionStampInterceptor>();
        services.TryAddScoped<IOutboxAdministrator, OutboxAdministrator<TDbContext>>();
        services.TryAddSingleton<IEventBroadcast>(
            serviceProvider => new InProcessEventBroadcast(
                serviceProvider.GetService<ILogger<InProcessEventBroadcast>>()
                    ?? NullLogger<InProcessEventBroadcast>.Instance
            )
        );
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IOptions<OutboxOptions>>(Options.Create(options.Outbox));

        RegisterHandlers(services, options.HandlerAssemblies, typeof(IDomainEventHandler<>));
        RegisterHandlers(services, options.HandlerAssemblies, typeof(IIntegrationEventHandler<>));

        if (options.EnableAuditAndDomainEventInterceptor) {
            services.TryAddScoped<AuditAndDomainEventInterceptor>();
        }

        if (options.EnableOutboxDispatcher) {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, OutboxDispatcherHostedService<TDbContext>>()
            );
        }

        services.ConfigureDbContext<TDbContext>(
            (serviceProvider, builder) => builder.AddInterceptors(
                CreateInterceptors(serviceProvider, options.EnableAuditAndDomainEventInterceptor)
            ));

        return services;
    }

    private static IInterceptor[] CreateInterceptors(
        IServiceProvider serviceProvider,
        bool enableAuditAndDomainEventInterceptor
    ) {
        if (enableAuditAndDomainEventInterceptor) {
            return [
                serviceProvider.GetRequiredService<AuditAndDomainEventInterceptor>(),
                serviceProvider.GetRequiredService<RowVersionStampInterceptor>()
            ];
        }

        return [serviceProvider.GetRequiredService<RowVersionStampInterceptor>()];
    }

    private static void RegisterHandlers(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        Type openHandlerType
    ) {
        foreach (Assembly assembly in assemblies.Distinct()) {
            IEnumerable<Type> implementationTypes = GetLoadableTypes(assembly)
                .Where(type => type is { IsAbstract: false, IsInterface: false });

            foreach (Type implementationType in implementationTypes) {
                IEnumerable<Type> serviceTypes = implementationType.GetInterfaces()
                    .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == openHandlerType);

                foreach (Type serviceType in serviceTypes) {
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
                }
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException exception) {
            // 組件內部分型別的相依組件無法載入時，略過這些型別，仍註冊其餘可載入的處理器。
            return exception.Types.OfType<Type>();
        }
    }
}
