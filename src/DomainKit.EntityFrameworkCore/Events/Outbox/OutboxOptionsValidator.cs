using Microsoft.Extensions.Options;

namespace CloudyWing.DomainKit.EntityFrameworkCore.Events.Outbox;

internal sealed class OutboxOptionsValidator : IValidateOptions<OutboxOptions> {
    public ValidateOptionsResult Validate(string? name, OutboxOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        IReadOnlyList<string> failures = GetFailures(options);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    internal static IReadOnlyList<string> GetFailures(OutboxOptions options) {
        List<string> failures = [];
        if (options.BatchSize <= 0) {
            failures.Add($"{nameof(OutboxOptions.BatchSize)} 必須大於 0。");
        }

        if (options.PollingInterval <= TimeSpan.Zero) {
            failures.Add($"{nameof(OutboxOptions.PollingInterval)} 必須大於 0。");
        }

        if (options.LockDuration <= TimeSpan.Zero) {
            failures.Add($"{nameof(OutboxOptions.LockDuration)} 必須大於 0。");
        }

        if (options.BaseRetryDelay <= TimeSpan.Zero) {
            failures.Add($"{nameof(OutboxOptions.BaseRetryDelay)} 必須大於 0。");
        }

        if (options.MaxRetryDelay < options.BaseRetryDelay) {
            failures.Add($"{nameof(OutboxOptions.MaxRetryDelay)} 不可小於 {nameof(OutboxOptions.BaseRetryDelay)}。");
        }

        if (options.MaxRetryCount < 0) {
            failures.Add($"{nameof(OutboxOptions.MaxRetryCount)} 不可小於 0。");
        }

        if (options.RetentionPeriod <= TimeSpan.Zero) {
            failures.Add($"{nameof(OutboxOptions.RetentionPeriod)} 必須大於 0。");
        }

        return failures;
    }
}
