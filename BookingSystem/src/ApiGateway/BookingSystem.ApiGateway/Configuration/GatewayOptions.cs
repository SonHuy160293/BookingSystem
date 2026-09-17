namespace BookingSystem.ApiGateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string[] AllowedHosts { get; set; } = [];
    public string[] KnownProxies { get; set; } = [];
    public long MaxRequestBodyBytes { get; set; } = 10 * 1024 * 1024;
    public int ConnectionTimeoutSeconds { get; set; } = 5;
    public int ActivityTimeoutSeconds { get; set; } = 15;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public Dictionary<string, GatewayRateLimitOptions> RateLimits { get; set; } = new(StringComparer.Ordinal)
    {
        [GatewayPolicies.Login] = new() { PermitLimit = 5, WindowSeconds = 600 },
        [GatewayPolicies.Refresh] = new() { PermitLimit = 60, WindowSeconds = 60 },
        [GatewayPolicies.AuthManagement] = new() { PermitLimit = 60, WindowSeconds = 60 },
        [GatewayPolicies.Read] = new() { PermitLimit = 120, WindowSeconds = 60 },
        [GatewayPolicies.Write] = new() { PermitLimit = 30, WindowSeconds = 60 }
    };
}

public sealed class GatewayRateLimitOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
