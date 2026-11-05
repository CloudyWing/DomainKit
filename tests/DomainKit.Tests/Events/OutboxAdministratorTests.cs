using CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Events;

[TestFixture]
public sealed class OutboxAdministratorTests {
    [Test]
    public async Task RequeueDeadLetteredAsync_DeadLetteredMessage_ResetsToPendingAsync() {
        DateTimeOffset now = new(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);
        await using TestDatabase database = await TestDatabase.CreateAsync(
            configureServices: services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(now))
        );
        Guid id = await AddMessageAsync(database, OutboxMessageStatus.DeadLettered, now.AddHours(-1));
        IOutboxAdministrator sut = database.Services.GetRequiredService<IOutboxAdministrator>();

        bool requeued = await sut.RequeueDeadLetteredAsync(id);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage message = await database.DbContext.Set<OutboxMessage>().SingleAsync(item => item.Id == id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(requeued, Is.True);
            Assert.That(message.Status, Is.EqualTo(OutboxMessageStatus.Pending));
            Assert.That(message.RetryCount, Is.Zero);
            Assert.That(message.NextRetryOn, Is.EqualTo(now));
            Assert.That(message.LockedBy, Is.Null);
            Assert.That(message.LockedUntil, Is.Null);
            Assert.That(message.LastError, Is.EqualTo("Previous failure."));
        }
    }

    [TestCase(OutboxMessageStatus.Pending)]
    [TestCase(OutboxMessageStatus.Processing)]
    [TestCase(OutboxMessageStatus.Processed)]
    public async Task RequeueDeadLetteredAsync_NotDeadLettered_ReturnsFalseAndKeepsStatusAsync(OutboxMessageStatus status) {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        Guid id = await AddMessageAsync(database, status, DateTimeOffset.UtcNow);
        IOutboxAdministrator sut = database.Services.GetRequiredService<IOutboxAdministrator>();

        bool requeued = await sut.RequeueDeadLetteredAsync(id);
        database.DbContext.ChangeTracker.Clear();
        OutboxMessage message = await database.DbContext.Set<OutboxMessage>().SingleAsync(item => item.Id == id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(requeued, Is.False);
            Assert.That(message.Status, Is.EqualTo(status));
        }
    }

    [Test]
    public async Task RequeueDeadLetteredAsync_UnknownId_ReturnsFalseAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IOutboxAdministrator sut = database.Services.GetRequiredService<IOutboxAdministrator>();

        bool requeued = await sut.RequeueDeadLetteredAsync(Guid.CreateVersion7());

        Assert.That(requeued, Is.False);
    }

    [Test]
    public async Task GetStatisticsAsync_MixedStatuses_ReturnsCountsAndOldestPendingAsync() {
        DateTimeOffset oldest = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await AddMessageAsync(database, OutboxMessageStatus.Pending, oldest.AddDays(1));
        await AddMessageAsync(database, OutboxMessageStatus.Pending, oldest);
        await AddMessageAsync(database, OutboxMessageStatus.Processing, oldest.AddDays(-1));
        await AddMessageAsync(database, OutboxMessageStatus.DeadLettered, oldest.AddDays(-2));
        IOutboxAdministrator sut = database.Services.GetRequiredService<IOutboxAdministrator>();

        OutboxStatistics actual = await sut.GetStatisticsAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.PendingCount, Is.EqualTo(2));
            Assert.That(actual.ProcessingCount, Is.EqualTo(1));
            Assert.That(actual.ProcessedCount, Is.Zero);
            Assert.That(actual.DeadLetteredCount, Is.EqualTo(1));
            Assert.That(actual.OldestPendingOccurredOn, Is.EqualTo(oldest));
        }
    }

    [Test]
    public async Task GetStatisticsAsync_EmptyOutbox_ReturnsZeroCountsAsync() {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        IOutboxAdministrator sut = database.Services.GetRequiredService<IOutboxAdministrator>();

        OutboxStatistics actual = await sut.GetStatisticsAsync();

        using (Assert.EnterMultipleScope()) {
            Assert.That(actual.PendingCount, Is.Zero);
            Assert.That(actual.DeadLetteredCount, Is.Zero);
            Assert.That(actual.OldestPendingOccurredOn, Is.Null);
        }
    }

    private static async Task<Guid> AddMessageAsync(
        TestDatabase database,
        OutboxMessageStatus status,
        DateTimeOffset occurredOn
    ) {
        Guid id = Guid.CreateVersion7();
        database.DbContext.Set<OutboxMessage>().Add(new OutboxMessage {
            Id = id,
            EventType = typeof(TestIntegrationEvent).AssemblyQualifiedName
                ?? throw new InvalidOperationException("測試事件缺少組件限定名稱。"),
            Payload = "{}",
            OccurredOn = occurredOn,
            Status = status,
            RetryCount = 9,
            NextRetryOn = occurredOn,
            LockedBy = "previous-instance",
            LockedUntil = occurredOn,
            LastError = "Previous failure."
        });
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        return id;
    }
}
