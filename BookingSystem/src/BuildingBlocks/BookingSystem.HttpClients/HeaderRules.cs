namespace BookingSystem.HttpClients;

internal static class HeaderRules
{
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "X-Integration-Key", "X-Correlation-Id",
        "Host", "traceparent", "tracestate", "baggage", "Cookie", "Set-Cookie",
        "Content-Length", "Content-Type", "Transfer-Encoding", "Connection", "Upgrade", "Trailer", "TE"
    };

    public static bool IsName(string? name)
        => !string.IsNullOrEmpty(name)
            && name.All(c => char.IsAsciiLetterOrDigit(c) || "!#$%&'*+-.^_`|~".Contains(c));

    public static bool IsValue(string? value)
        => value is not null && !value.Any(char.IsControl);

    public static bool IsProtected(string name, string? apiKeyHeader = null)
        => ProtectedNames.Contains(name) || string.Equals(name, apiKeyHeader, StringComparison.OrdinalIgnoreCase);
}
