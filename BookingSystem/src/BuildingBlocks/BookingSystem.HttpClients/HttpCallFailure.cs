namespace BookingSystem.HttpClients;

public enum HttpCallFailureKind
{
    HttpFailure,
    NetworkFailure,
    Timeout,
    CircuitOpen,
    InvalidResponse,
    AuthenticationFailure
}

public sealed record HttpCallFailure(HttpCallFailureKind Kind, string Code, string Message, TimeSpan? RetryAfter = null)
{
    internal static HttpCallFailure Create(HttpCallFailureKind kind, TimeSpan? retryAfter = null)
        => new(kind, "HttpDependency." + kind, kind switch
        {
            HttpCallFailureKind.HttpFailure => "The dependency returned an unsuccessful HTTP response.",
            HttpCallFailureKind.NetworkFailure => "The dependency could not be reached.",
            HttpCallFailureKind.Timeout => "The dependency call timed out.",
            HttpCallFailureKind.CircuitOpen => "The dependency is temporarily unavailable.",
            HttpCallFailureKind.InvalidResponse => "The dependency returned an invalid response.",
            HttpCallFailureKind.AuthenticationFailure => "The service credential could not be obtained.",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }, retryAfter);
}
