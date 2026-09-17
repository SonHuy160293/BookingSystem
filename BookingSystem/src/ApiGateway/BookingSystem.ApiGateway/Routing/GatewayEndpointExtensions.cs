using BookingSystem.ApiGateway.Configuration;
using BookingSystem.ApiGateway.Middleware;
using BookingSystem.SharedKernel.Abstractions.Shared;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BookingSystem.ApiGateway.Routing;

public static class GatewayEndpointExtensions
{
    public static void MapGatewayEndpoints(this WebApplication app, IConfiguration proxyConfiguration)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteHealthAsync
        }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteHealthAsync
        }).AllowAnonymous();

        // Retain YARP's default load-balancing and health-check middleware.
        app.MapReverseProxy();
        MapMethodFallbacks(app, proxyConfiguration);
        app.MapFallback("/{**path}", context => GatewayErrorWriter.WriteAsync(context, StatusCodes.Status404NotFound))
            .AllowAnonymous();
    }

    private static void MapMethodFallbacks(IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        var groups = configuration.GetSection("Routes").GetChildren()
            .GroupBy(route => route["Match:Path"]!, StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var first = group.First();
            string allow = string.Join(", ", group.SelectMany(route =>
                route.GetSection("Match:Methods").Get<string[]>() ?? []).Distinct(StringComparer.OrdinalIgnoreCase));
            var endpoint = endpoints.Map(group.Key, async context =>
            {
                context.Response.Headers.Allow = allow;
                await GatewayErrorWriter.WriteAsync(context, StatusCodes.Status405MethodNotAllowed);
            });
            endpoint.Add(builder => ((RouteEndpointBuilder)builder).Order = 1000);
            endpoint.WithMetadata(new GatewayEndpointMetadata(first.Key + "-method", first["ClusterId"]!));
            endpoint.RequireCors(GatewayPolicies.Cors);
            endpoint.RequireRateLimiting(first["RateLimiterPolicy"]!);
            endpoint.WithRequestTimeout(GatewayPolicies.Timeout);
            if (first["AuthorizationPolicy"] == "anonymous")
            {
                endpoint.AllowAnonymous();
            }
            else
            {
                endpoint.RequireAuthorization(GatewayPolicies.Jwt);
            }
        }
    }

    private static Task WriteHealthAsync(HttpContext context, HealthReport report)
    {
        context.Response.Headers.CacheControl = "no-store";
        return report.Status == HealthStatus.Healthy
            ? context.Response.WriteAsJsonAsync(ApiResponse.Ok(new { status = "Healthy" }), cancellationToken: context.RequestAborted)
            : GatewayErrorWriter.WriteAsync(context, StatusCodes.Status503ServiceUnavailable);
    }
}
