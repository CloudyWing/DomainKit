using System.Reflection;
using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

namespace CloudyWing.DomainKit.EntityFrameworkCore.DependencyInjection;

/// <summary>
/// 表示 DomainKit EF Core 註冊選項。
/// </summary>
public sealed class DomainKitOptions {
    /// <summary>
    /// 取得要掃描事件處理器的組件集合。
    /// </summary>
    public ICollection<Assembly> HandlerAssemblies { get; } = [];

    /// <summary>
    /// 取得或設定一個值，用以指出是否註冊審計與事件 interceptor。
    /// </summary>
    public bool EnableAuditAndDomainEventInterceptor { get; set; } = true;

    /// <summary>
    /// 取得或設定一個值，用以指出是否啟動 Outbox dispatcher。
    /// </summary>
    public bool EnableOutboxDispatcher { get; set; }

    /// <summary>
    /// 取得 Outbox dispatcher 選項。
    /// </summary>
    public OutboxOptions Outbox { get; } = new();
}
