using Microsoft.EntityFrameworkCore.Storage;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 定義持久化工作單元的提交與交易封裝能力。
/// </summary>
public interface IUnitOfWork {
    /// <summary>
    /// 儲存目前工作單元的異動。
    /// </summary>
    /// <param name="actorId">審計使用者識別碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>寫入資料庫的狀態項目數。</returns>
    Task<int> SaveChangesAsync(Guid? actorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 捨棄目前工作單元中尚未提交的異動與待派發事件。
    /// </summary>
    void DiscardChanges();

    /// <summary>
    /// 在交易中執行指定作業。
    /// </summary>
    /// <param name="executor">交易內要執行的作業。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    Task UseTransactionAsync(Func<CancellationToken, Task> executor, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在交易中執行指定作業並回傳結果。
    /// </summary>
    /// <typeparam name="TResult">結果型別。</typeparam>
    /// <param name="executor">交易內要執行的作業。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>作業結果。</returns>
    Task<TResult> UseTransactionAsync<TResult>(
        Func<IDbContextTransaction, CancellationToken, Task<TResult>> executor,
        CancellationToken cancellationToken = default);
}
