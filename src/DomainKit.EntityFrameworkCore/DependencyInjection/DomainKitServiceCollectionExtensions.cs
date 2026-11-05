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
    /// <remarks>
    /// <see cref="OutboxOptions" /> 以 <see cref="DomainKitOptions.Outbox" /> 的值直接註冊為 <see cref="IOptions{TOptions}" />，
    /// 不經過 Options pipeline。需要 configuration 綁定或 <c>ValidateOnStart</c> 時，改用接受
    /// <see cref="OptionsBuilder{TOptions}" /> 設定函式的多載。
    /// </remarks>
    public static IServiceCollection AddDomainKit<TDbContext>(
        this IServiceCollection services,
        Action<DomainKitOptions>? configure = null,
        params Assembly[] handlerAssemblies
    ) where TDbContext : DbContext {
        ArgumentNullException.ThrowIfNull(services);

        DomainKitOptions options = CreateOptions(configure, handlerAssemblies);
        services.TryAddSingleton<IOptions<OutboxOptions>>(Options.Create(options.Outbox));

        return AddDomainKitCore<TDbContext>(services, options);
    }

    /// <summary>
    /// 註冊 DomainKit EF Core 基礎服務，並透過 Options pipeline 設定與驗證 <see cref="OutboxOptions" />。
    /// </summary>
    /// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
    /// <param name="services">服務集合。</param>
    /// <param name="configure">選項設定函式。</param>
    /// <param name="configureOutboxOptions">
    /// <see cref="OutboxOptions" /> 的 Options 建置器設定函式，可呼叫 <c>Bind</c>、<c>BindConfiguration</c>、<c>Validate</c> 或 <c>ValidateOnStart</c>。
    /// </param>
    /// <param name="handlerAssemblies">要掃描事件處理器的組件集合。</param>
    /// <returns>服務集合。</returns>
    /// <remarks>
    /// <see cref="DomainKitOptions.Outbox" /> 的值會先作為預設值套用，之後才執行 <paramref name="configureOutboxOptions" /> 加入的設定。
    /// DomainKit 會註冊 <see cref="IValidateOptions{TOptions}" /> 驗證各選項的有效範圍；取得選項值時驗證失敗會拋出 <see cref="OptionsValidationException" />。
    /// </remarks>
    public static IServiceCollection AddDomainKit<TDbContext>(
        this IServiceCollection services,
        Action<DomainKitOptions>? configure,
        Action<OptionsBuilder<OutboxOptions>> configureOutboxOptions,
        params Assembly[] handlerAssemblies
    ) where TDbContext : DbContext {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOutboxOptions);

        DomainKitOptions options = CreateOptions(configure, handlerAssemblies);
        OptionsBuilder<OutboxOptions> outboxOptionsBuilder = services.AddOptions<OutboxOptions>()
            .Configure(target => CopyOutboxOptions(options.Outbox, target));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OutboxOptions>, OutboxOptionsValidator>());
        configureOutboxOptions(outboxOptionsBuilder);

        return AddDomainKitCore<TDbContext>(services, options);
    }

    private static DomainKitOptions CreateOptions(Action<DomainKitOptions>? configure, Assembly[] handlerAssemblies) {
        DomainKitOptions options = new();
        configure?.Invoke(options);

        foreach (Assembly handlerAssembly in handlerAssemblies) {
            options.HandlerAssemblies.Add(handlerAssembly);
        }

        return options;
    }

    private static void CopyOutboxOptions(OutboxOptions source, OutboxOptions target) {
        target.PollingInterval = source.PollingInterval;
        target.BatchSize = source.BatchSize;
        target.LockDuration = source.LockDuration;
        target.BaseRetryDelay = source.BaseRetryDelay;
        target.MaxRetryDelay = source.MaxRetryDelay;
        target.MaxRetryCount = source.MaxRetryCount;
        target.RetentionPeriod = source.RetentionPeriod;
    }

    private static IServiceCollection AddDomainKitCore<TDbContext>(
        IServiceCollection services,
        DomainKitOptions options
    ) where TDbContext : DbContext {
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
