using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 提供 <see cref="IUnitOfWork" /> 的交易擴充方法。
/// </summary>
public static class UnitOfWorkExtensions {
    /// <summary>
    /// 在 EF Core execution strategy 內以交易執行指定作業，暫時性失敗時由 execution strategy 重試整個交易。
    /// </summary>
    /// <param name="unitOfWork">工作單元。</param>
    /// <param name="executor">交易內要執行的作業；重試時會再次執行，須可安全重複執行。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步作業。</returns>
    /// <exception cref="NotSupportedException"><paramref name="unitOfWork" /> 不是 DomainKit 的 <see cref="UnitOfWork{TDbContext}" /> 時。</exception>
    /// <exception cref="InvalidOperationException">execution strategy 具重試能力且 DbContext 已有未儲存的變更時。</exception>
    /// <remarks>
    /// 只能在尚未開啟交易時呼叫；已有交易時，具重試能力的 execution strategy 會拒絕執行。
    /// 每次重試前會呼叫 <see cref="IUnitOfWork.DiscardChanges" />，清除 DbContext 的所有追蹤狀態與待派發事件。
    /// execution strategy 具重試能力且 DbContext 在呼叫前已有未儲存的變更時，會於執行前擲出例外；
    /// <paramref name="executor" /> 應在內部重新載入並修改所需資料。
    /// </remarks>
    public static Task UseResilientTransactionAsync(
        this IUnitOfWork unitOfWork,
        Func<CancellationToken, Task> executor,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(executor);

        return unitOfWork.UseResilientTransactionAsync(
            async (_, ct) => {
                await executor(ct).ConfigureAwait(false);

                return true;
            },
            cancellationToken
        );
    }

    /// <summary>
    /// 在 EF Core execution strategy 內以交易執行指定作業並回傳結果，暫時性失敗時由 execution strategy 重試整個交易。
    /// </summary>
    /// <typeparam name="TResult">結果型別。</typeparam>
    /// <param name="unitOfWork">工作單元。</param>
    /// <param name="executor">交易內要執行的作業；重試時會再次執行，須可安全重複執行。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>作業結果。</returns>
    /// <exception cref="NotSupportedException"><paramref name="unitOfWork" /> 不是 DomainKit 的 <see cref="UnitOfWork{TDbContext}" /> 時。</exception>
    /// <exception cref="InvalidOperationException">execution strategy 具重試能力且 DbContext 已有未儲存的變更時。</exception>
    /// <remarks>
    /// 只能在尚未開啟交易時呼叫；已有交易時，具重試能力的 execution strategy 會拒絕執行。
    /// 每次重試前會呼叫 <see cref="IUnitOfWork.DiscardChanges" />，清除 DbContext 的所有追蹤狀態與待派發事件。
    /// execution strategy 具重試能力且 DbContext 在呼叫前已有未儲存的變更時，會於執行前擲出例外；
    /// <paramref name="executor" /> 應在內部重新載入並修改所需資料。
    /// </remarks>
    public static async Task<TResult> UseResilientTransactionAsync<TResult>(
        this IUnitOfWork unitOfWork,
        Func<IDbContextTransaction, CancellationToken, Task<TResult>> executor,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(executor);

        if (unitOfWork is not IDbContextProvider dbContextProvider) {
            throw new NotSupportedException(
                $"{nameof(UseResilientTransactionAsync)} 需要 DomainKit 的 UnitOfWork<TDbContext> 實作以取得 execution strategy。"
            );
        }

        IExecutionStrategy strategy = dbContextProvider.DbContext.Database.CreateExecutionStrategy();
        if (strategy.RetriesOnFailure && dbContextProvider.DbContext.ChangeTracker.HasChanges()) {
            throw new InvalidOperationException(
                $"{nameof(UseResilientTransactionAsync)} 重試時會清除追蹤狀態，呼叫前不可有未儲存的變更，請先儲存或將修改移入 executor。"
            );
        }

        bool isRetry = false;

        return await strategy.ExecuteAsync(
            async ct => {
                if (isRetry) {
                    unitOfWork.DiscardChanges();
                }

                isRetry = true;

                return await unitOfWork.UseTransactionAsync(executor, ct).ConfigureAwait(false);
            },
            cancellationToken
        ).ConfigureAwait(false);
    }
}
