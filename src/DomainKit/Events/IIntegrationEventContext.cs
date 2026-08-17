namespace CloudyWing.DomainKit.Events;

/// <summary>
/// 提供目前整合事件的派發內容。
/// </summary>
public interface IIntegrationEventContext {
    /// <summary>
    /// 取得目前事件識別碼。
    /// </summary>
    Guid EventId { get; }
}
