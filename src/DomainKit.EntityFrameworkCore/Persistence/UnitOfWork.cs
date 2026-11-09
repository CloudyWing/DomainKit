using CloudyWing.DomainKit.EntityFrameworkCore.Auditing;
using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Persistence;

/// <summary>
/// 以 EF Core <see cref="DbContext" /> 實作工作單元。
/// </summary>
/// <typeparam name="TDbContext">資料庫內容型別。</typeparam>
public sealed class UnitOfWork<TDbContext> : IUnitOfWork, IDbContextProvider where TDbContext : DbContext {
    private readonly TDbContext dbContext;

    /// <summary>
    /// 初始化 <see cref="UnitOfWork{TDbContext}" /> 類別的新執行個體。
    /// </summary>
    /// <param name="dbContext">資料庫內容。</param>
    public UnitOfWork(TDbContext dbContext) {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    DbContext IDbContextProvider.DbContext => dbContext;

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(Guid? actorId = null, CancellationToken cancellationToken = default) {
        using IDisposable scope = AuditScope.Begin(actorId);

        try {
            return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (BusinessException) {
            DiscardChanges();
            throw;
        } catch (DbUpdateConcurrencyException exception) {
            throw new ConcurrencyConflictException(exception);
        }
    }

    /// <inheritdoc />
    public void DiscardChanges() {
        foreach (IAggregateRoot aggregateRoot in dbContext.ChangeTracker.Entries()
                     .Select(entry => entry.Entity)
                     .OfType<IAggregateRoot>()
                     .ToList()) {
            aggregateRoot.ClearDomainEvents();
            aggregateRoot.ClearIntegrationEvents();
        }

        dbContext.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public async Task UseTransactionAsync(
        Func<CancellationToken, Task> executor,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(executor);

        await UseTransactionAsync(
            async (_, ct) => {
                await executor(ct).ConfigureAwait(false);

                return true;
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResult> UseTransactionAsync<TResult>(
        Func<IDbContextTransaction, CancellationToken, Task<TResult>> executor,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(executor);

        IDbContextTransaction? currentTransaction = dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null) {
            return await executor(currentTransaction, cancellationToken).ConfigureAwait(false);
        }

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try {
            TResult result = await executor(transaction, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        } catch {
            // 取消時仍須完成 rollback，並保留原始例外，不讓已取消的權杖改拋 OperationCanceledException。
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
