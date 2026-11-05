using CloudyWing.DomainKit.EntityFrameworkCore.Persistence;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Persistence;

[TestFixture]
public sealed class UnitOfWorkExtensionsTests {
    [Test]
    public async Task UseResilientTransactionAsync_Success_CommitsChangesAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();

        await sut.UseResilientTransactionAsync(async cancellationToken => {
            database.DbContext.HardDeleteEntities.Add(new HardDeleteEntity { Name = "Resilient" });
            await sut.SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        });
        int count = await database.DbContext.HardDeleteEntities.AsNoTracking().CountAsync();

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UseResilientTransactionAsync_ExecutorThrows_RollsBackAndRethrowsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();

        Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.UseResilientTransactionAsync(
            async cancellationToken => {
                database.DbContext.HardDeleteEntities.Add(new HardDeleteEntity { Name = "Rollback" });
                await sut.SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("Rollback.");
            }
        ));
        database.DbContext.ChangeTracker.Clear();
        int count = await database.DbContext.HardDeleteEntities.AsNoTracking().CountAsync();

        Assert.That(count, Is.Zero);
    }

    [Test]
    public async Task UseResilientTransactionAsync_TransientFailure_RetriesWholeTransactionAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync(
            configureDbContext: builder => builder.UseSqlite(
                sqliteOptions => sqliteOptions.ExecutionStrategy(
                    dependencies => new TimeoutRetryingExecutionStrategy(dependencies)
                )
            )
        );
        IUnitOfWork sut = database.Services.GetRequiredService<IUnitOfWork>();
        int attempts = 0;

        string result = await sut.UseResilientTransactionAsync(
            async (_, cancellationToken) => {
                attempts++;
                database.DbContext.HardDeleteEntities.Add(new HardDeleteEntity { Name = $"Attempt-{attempts}" });
                await sut.SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (attempts == 1) {
                    throw new TimeoutException("Transient failure.");
                }

                return "done";
            }
        );
        List<string> names = await database.DbContext.HardDeleteEntities
            .AsNoTracking()
            .Select(entity => entity.Name)
            .ToListAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(result, Is.EqualTo("done"));
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(names, Is.EqualTo(new[] { "Attempt-2" }));
        }
    }

    [Test]
    public void UseResilientTransactionAsync_ForeignUnitOfWork_ThrowsNotSupportedException() {
        IUnitOfWork sut = Substitute.For<IUnitOfWork>();

        Assert.ThrowsAsync<NotSupportedException>(async () => await sut.UseResilientTransactionAsync(
            _ => Task.CompletedTask
        ));
    }

    private sealed class TimeoutRetryingExecutionStrategy : ExecutionStrategy {
        public TimeoutRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies, 3, TimeSpan.Zero) {
        }

        protected override bool ShouldRetryOn(Exception exception) {
            return exception is TimeoutException;
        }
    }
}
