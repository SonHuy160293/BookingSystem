using BookingSystem.ApiGateway.Logging;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace BookingSystem.ApiGateway.Configuration;

public sealed class GatewayProxyConfigFilter : IProxyConfigFilter
{
    private readonly GatewayOptions _options;
    private static readonly HashSet<string> IdentityResources = ["Users", "Roles", "Permissions", "Tenants", "Branches"];
    private static readonly HashSet<string> CinemaResources = ["Cinemas", "Films", "ScreeningRooms"];

    public GatewayProxyConfigFilter(IOptions<GatewayOptions> options)
    {
        _options = options.Value;
    }

    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        if (cluster.ClusterId is not ("identity-api" or "cinema-api")
            || cluster.Destinations is null || cluster.Destinations.Count == 0
            || cluster.Destinations.Values.Any(destination =>
                !Uri.TryCreate(destination.Address, UriKind.Absolute, out var address)
                || address.Scheme is not ("http" or "https"))
            || cluster.HealthCheck?.Active?.Enabled != true
            || cluster.HealthCheck.Active.Path != "/health/ready")
        {
            throw new InvalidOperationException("Gateway cluster configuration is invalid.");
        }

        return ValueTask.FromResult(cluster with
        {
            HttpRequest = (cluster.HttpRequest ?? new ForwarderRequestConfig()) with
            {
                ActivityTimeout = TimeSpan.FromSeconds(_options.ActivityTimeoutSeconds)
            }
        });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        string prefix = route.ClusterId == "identity-api" ? "/identity" : "/cinema";
        string path = route.Match.Path ?? string.Empty;
        bool publicAuth = path is "/identity/api/Auth/login" or "/identity/api/Auth/refresh" or "/identity/api/Auth/logout";
        bool authManagement = path is "/identity/api/Auth/logout-all" or "/identity/api/Auth/sessions"
            or "/identity/api/Auth/sessions/{sessionId:guid}";
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        bool resource = segments.Length == 5 && segments[1] == "api" && segments[2] == "v1"
            && segments[4] == "{**remainder}"
            && (prefix == "/identity" ? IdentityResources : CinemaResources).Contains(segments[3]);
        bool prefixValid = route.Metadata is not null && route.Metadata.TryGetValue("PublicPrefix", out string? configuredPrefix)
            && configuredPrefix == prefix && path.StartsWith(prefix + "/api/", StringComparison.Ordinal);
        bool removesPrefix = route.Transforms?.Any(transform => transform.TryGetValue("PathRemovePrefix", out string? value) && value == prefix) == true;

        if (cluster is null || !GatewaySafeLogFilter.IsSafeToken(route.RouteId)
            || !prefixValid || !removesPrefix || !(publicAuth || authManagement || resource)
            || route.Match.Methods is null || route.Match.Methods.Count == 0
            || route.Match.Methods.Any(method => method is not ("GET" or "POST" or "PUT" or "DELETE"))
            || publicAuth && (route.Match.Methods.Count != 1 || route.Match.Methods[0] != "POST")
            || route.AuthorizationPolicy != (publicAuth ? "anonymous" : GatewayPolicies.Jwt)
            || route.CorsPolicy != GatewayPolicies.Cors
            || route.Timeout.HasValue || route.TimeoutPolicy != GatewayPolicies.Timeout
            || route.RateLimiterPolicy is null || !GatewayPolicies.RateLimitNames.Contains(route.RateLimiterPolicy))
        {
            throw new InvalidOperationException("Gateway route configuration is invalid.");
        }

        return ValueTask.FromResult(route with { MaxRequestBodySize = _options.MaxRequestBodyBytes });
    }
}
