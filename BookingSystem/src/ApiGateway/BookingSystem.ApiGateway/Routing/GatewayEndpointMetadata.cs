using Yarp.ReverseProxy.Model;

namespace BookingSystem.ApiGateway.Routing;

public sealed record GatewayEndpointMetadata(string RouteId, string ClusterId)
{
    public static GatewayEndpointMetadata From(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var route = endpoint?.Metadata.GetMetadata<RouteModel>();
        return route is not null
            ? new(route.Config.RouteId, route.Config.ClusterId ?? "None")
            : endpoint?.Metadata.GetMetadata<GatewayEndpointMetadata>() ?? new("unmatched", "None");
    }
}
