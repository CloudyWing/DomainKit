namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

/// <summary>
/// 定義 Outbox 的維運管理操作。
/// </summary>
/// <remarks>
/// 操作會略過全域查詢篩選，跨租戶存取全部 Outbox 訊息，只應開放給受控的維運流程。
/// 使用前 DbContext 必須已呼叫 <see cref="OutboxModelBuilderExtensions.ConfigureOutbox" />。
/// </remarks>
public interface IOutboxAdministrator {
    /// <summary>
    /// 將已進入 Dead Letter 的訊息重新排入待派發佇列。
    /// </summary>
    /// <param name="id">Outbox 訊息識別碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若訊息存在且原本為 <see cref="OutboxMessageStatus.DeadLettered" /> 並已重新排入則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    /// <remarks>
    /// 重新排入時會將重試次數歸零、清除租約並設定為立即可派發；<see cref="OutboxMessage.LastError" /> 保留供追查。
    /// </remarks>
    Task<bool> RequeueDeadLetteredAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得 Outbox 各狀態的訊息統計。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>Outbox 統計資訊。</returns>
    Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
