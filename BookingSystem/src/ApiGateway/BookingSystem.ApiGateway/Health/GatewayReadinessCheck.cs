using Microsoft.Extensions.Diagnostics.HealthChecks;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Model;

namespace BookingSystem.ApiGateway.Health;

public sealed class GatewayReadinessCheck : IHealthCheck
{
    private readonly IProxyStateLookup _proxy;

    public GatewayReadinessCheck(IProxyStateLookup proxy)
    {
        _proxy = proxy;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (string id in new[] { "identity-api", "cinema-api" })
        {
            if (!_proxy.TryGetCluster(id, out var cluster)
                || !cluster.Destinations.Values.Any(destination =>
                    destination.Health.Active == DestinationHealth.Healthy
                    && destination.Health.Passive != DestinationHealth.Unhealthy))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("An upstream service is not ready."));
            }
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
