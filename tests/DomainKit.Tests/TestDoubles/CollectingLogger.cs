using Microsoft.Extensions.Logging;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class CollectingLogger<T> : ILogger<T> {
    public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) {
        Entries.Add((logLevel, exception));
    }
}
