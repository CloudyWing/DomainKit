using CloudyWing.DomainKit.EntityFrameworkCore.Events.Broadcast;
using CloudyWing.DomainKit.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace CloudyWing.DomainKit.Tests.Events;

[TestFixture]
public sealed class InProcessEventBroadcastTests {
    [Test]
    public async Task PublishAsync_Subscribed_InvokesHandlerAsync() {
        InProcessEventBroadcast sut = new();
        TestIntegrationEvent payload = new(Guid.CreateVersion7());
        TestIntegrationEvent? received = null;
        using IDisposable subscription = sut.Subscribe<TestIntegrationEvent>((notification, _) => {
            received = notification;
            return Task.CompletedTask;
        });

        await sut.PublishAsync(payload);

        Assert.That(received, Is.SameAs(payload));
    }

    [Test]
    public async Task PublishAsync_SubscriptionDisposed_DoesNotInvokeHandlerAsync() {
        InProcessEventBroadcast sut = new();
        int received = 0;
        IDisposable subscription = sut.Subscribe<TestIntegrationEvent>((_, _) => {
            received++;
            return Task.CompletedTask;
        });
        subscription.Dispose();

        await sut.PublishAsync(new TestIntegrationEvent(Guid.CreateVersion7()));

        Assert.That(received, Is.Zero);
    }

    [Test]
    public async Task PublishAsync_DisposeTwice_KeepsOtherSubscriptionAsync() {
        InProcessEventBroadcast sut = new();
        int received = 0;
        IDisposable first = sut.Subscribe<TestIntegrationEvent>((_, _) => Task.CompletedTask);
        using IDisposable second = sut.Subscribe<TestIntegrationEvent>((_, _) => {
            received++;
            return Task.CompletedTask;
        });
        first.Dispose();
        first.Dispose();

        await sut.PublishAsync(new TestIntegrationEvent(Guid.CreateVersion7()));

        Assert.That(received, Is.EqualTo(1));
    }

    [Test]
    public async Task PublishAsync_SubscriberThrows_LogsWarningAndContinuesAsync() {
        CollectingLogger<InProcessEventBroadcast> logger = new();
        InProcessEventBroadcast sut = new(logger);
        InvalidOperationException failure = new("Simulated subscriber failure.");
        int received = 0;
        using IDisposable failing = sut.Subscribe<TestIntegrationEvent>((_, _) => throw failure);
        using IDisposable succeeding = sut.Subscribe<TestIntegrationEvent>((_, _) => {
            received++;
            return Task.CompletedTask;
        });

        await sut.PublishAsync(new TestIntegrationEvent(Guid.CreateVersion7()));

        using (Assert.EnterMultipleScope()) {
            Assert.That(received, Is.EqualTo(1));
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(logger.Entries[0].Exception, Is.SameAs(failure));
        }
    }

    [Test]
    public void PublishAsync_CanceledTokenAndHandlerCanceled_PropagatesCancellation() {
        InProcessEventBroadcast sut = new();
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        using IDisposable subscription = sut.Subscribe<TestIntegrationEvent>(
            (_, cancellationToken) => Task.FromCanceled(cancellationToken)
        );

        Assert.CatchAsync<OperationCanceledException>(async () => await sut.PublishAsync(
            new TestIntegrationEvent(Guid.CreateVersion7()),
            cancellationTokenSource.Token
        ));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => _ = new InProcessEventBroadcast(null!));
    }
}
