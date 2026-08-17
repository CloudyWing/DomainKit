using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

internal static class DomainKitDiagnostics {
    public const string Name = "CloudyWing.DomainKit";

    public static readonly ActivitySource ActivitySource = new(Name);
    public static readonly Meter Meter = new(Name);
    public static readonly Histogram<double> OutboxDispatchDuration = Meter.CreateHistogram<double>(
        "domainkit.outbox.dispatch.duration",
        "ms"
    );
    public static readonly Counter<long> OutboxDeadLetter = Meter.CreateCounter<long>("domainkit.outbox.deadletter");
    private static long pendingCount;

    static DomainKitDiagnostics() {
        Meter.CreateObservableGauge("domainkit.outbox.pending", () => Interlocked.Read(ref pendingCount));
    }

    public static void SetPendingCount(long count) {
        Interlocked.Exchange(ref pendingCount, count);
    }
}
