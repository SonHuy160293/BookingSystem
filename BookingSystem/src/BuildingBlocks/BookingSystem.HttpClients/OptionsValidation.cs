using Microsoft.Extensions.Options;

namespace BookingSystem.HttpClients;

internal static class OptionsValidation
{
    public static bool IsDependencyName(string name)
        => name.Length is > 0 and <= 100
            && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-');

    public static void Validate(string name, HttpApiClientOptions options, bool isInternal)
    {
        string[] errors = Errors(name, options, isInternal).ToArray();
        if (errors.Length > 0)
        {
            throw new OptionsValidationException(name, typeof(HttpApiClientOptions), errors);
        }
    }

    public static IEnumerable<string> Errors(string name, HttpApiClientOptions options, bool isInternal)
    {
        if (!IsDependencyName(name))
        {
            yield return "Dependency name must be a bounded ASCII identifier.";
        }

        if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host)
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
        {
            yield return "BaseAddress must be an absolute HTTP(S) URL without credentials, query or fragment.";
        }

        if (isInternal)
        {
            if (string.IsNullOrWhiteSpace(options.IntegrationKey) || !HeaderRules.IsValue(options.IntegrationKey))
            {
                yield return "Internal clients require a valid IntegrationKey.";
            }

            if (options.ApiKey is not null || options.ApiKeyHeaderName is not null)
            {
                yield return "Internal clients use IntegrationKey, not external API-key settings.";
            }
        }
        else
        {
            if (options.IntegrationKey is not null)
            {
                yield return "External clients cannot configure an internal IntegrationKey.";
            }

            if (options.ApiKey is not null || options.ApiKeyHeaderName is not null)
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey) || !HeaderRules.IsValue(options.ApiKey)
                    || !HeaderRules.IsName(options.ApiKeyHeaderName)
                    || HeaderRules.IsProtected(options.ApiKeyHeaderName!))
                {
                    yield return "External API-key authentication requires a valid key and a nonreserved header name.";
                }
            }
        }

        if (!SecondsToDay(options.TotalTimeout) || !SecondsToDay(options.AttemptTimeout)
            || !SecondsToDay(options.ConnectTimeout) || options.ConnectTimeout > options.AttemptTimeout
            || options.AttemptTimeout > options.TotalTimeout)
        {
            yield return "Timeouts must be between one second and one day, with Connect <= Attempt <= Total.";
        }

        if (!SecondsToDay(options.HandlerLifetime) || !SecondsToDay(options.PooledConnectionLifetime)
            || !SecondsToDay(options.PooledConnectionIdleTimeout) || options.MaxConnectionsPerServer < 1)
        {
            yield return "Pooling durations must be between one second and one day; connection count must be positive.";
        }

        if (options.MaxRetryAttempts < 0 || options.RetryDelay < TimeSpan.Zero
            || options.MaxRetryDelay < options.RetryDelay || options.MaxRetryDelay > TimeSpan.FromDays(1))
        {
            yield return "Retry count and delays must be nonnegative, with Delay <= MaxDelay <= one day.";
        }

        if (!double.IsFinite(options.CircuitFailureRatio) || options.CircuitFailureRatio <= 0
            || options.CircuitFailureRatio > 1 || options.CircuitMinimumThroughput < 2
            || !SecondsToDay(options.CircuitSamplingDuration) || !SecondsToDay(options.CircuitBreakDuration)
            || options.CircuitSamplingDuration.TotalSeconds < options.AttemptTimeout.TotalSeconds * 2)
        {
            yield return "Circuit settings require a ratio in (0,1], throughput >= 2, bounded durations and Sampling >= 2 * Attempt.";
        }

        if (options.JsonOptions is null)
        {
            yield return "JsonOptions is required.";
        }
    }

    private static bool SecondsToDay(TimeSpan value)
        => value >= TimeSpan.FromSeconds(1) && value <= TimeSpan.FromDays(1);
}
