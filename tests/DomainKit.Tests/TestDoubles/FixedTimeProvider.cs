namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class FixedTimeProvider : TimeProvider {
    private readonly DateTimeOffset utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow) {
        this.utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() {
        return utcNow;
    }
}
