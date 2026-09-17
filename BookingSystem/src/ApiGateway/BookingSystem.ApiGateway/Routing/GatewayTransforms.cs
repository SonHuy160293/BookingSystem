using BookingSystem.ApiGateway.Middleware;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace BookingSystem.ApiGateway.Routing;

public static class GatewayTransforms
{
    public static void Apply(TransformBuilderContext builder)
    {
        string prefix = builder.Route.Metadata!["PublicPrefix"];
        builder.AddRequestTransform(context =>
        {
            var state = context.HttpContext.Features.Get<GatewayRequestState>()!;
            state.UpstreamOrigin = new Uri(context.DestinationPrefix);
            foreach (string name in context.ProxyRequest.Headers.Select(header => header.Key).ToArray())
            {
                if (ShouldRemove(name))
                {
                    context.ProxyRequest.Headers.Remove(name);
                }
            }

            // The forwarding handler injects the current W3C context after transforms.
            context.ProxyRequest.Headers.Host = null;
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", context.HttpContext.TraceIdentifier);
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.HttpContext.Request.Scheme);
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Host", context.HttpContext.Request.Host.Value);
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", prefix);
            if (context.HttpContext.Connection.RemoteIpAddress is { } address)
            {
                string ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
                context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
            }

            return ValueTask.CompletedTask;
        });

        builder.AddResponseTransform(context =>
        {
            if (context.ProxyResponse is null)
            {
                return ValueTask.CompletedTask;
            }

            var state = context.HttpContext.Features.Get<GatewayRequestState>()!;
            state.UpstreamResponseReceived = true;
            foreach (string name in context.ProxyResponse.Headers.Select(header => header.Key).ToArray())
            {
                if (name.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase))
                {
                    context.ProxyResponse.Headers.Remove(name);
                }
            }

            foreach (string name in context.HttpContext.Response.Headers.Keys.ToArray())
            {
                if (name.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase))
                {
                    context.HttpContext.Response.Headers.Remove(name);
                }
            }

            string location = context.HttpContext.Response.Headers.Location.ToString();
            if (location.Length == 0)
            {
                location = context.ProxyResponse.Headers.Location?.ToString() ?? string.Empty;
            }

            string? rewritten = RewriteLocation(location, state.UpstreamOrigin, context.HttpContext, prefix);
            if (rewritten is not null)
            {
                context.HttpContext.Response.Headers.Location = rewritten;
                context.ProxyResponse.Headers.Location = new Uri(rewritten, UriKind.Relative);
            }

            return ValueTask.CompletedTask;
        });
    }

    private static bool ShouldRemove(string name)
        => name.Equals("Forwarded", StringComparison.OrdinalIgnoreCase)
            || name.Equals("baggage", StringComparison.OrdinalIgnoreCase)
            || name.Equals("traceparent", StringComparison.OrdinalIgnoreCase)
            || name.Equals("tracestate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase)
            || name.Equals("X-Integration-Key", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Original-", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-User", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Tenant", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Branch", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Role", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Permission", StringComparison.OrdinalIgnoreCase);

    private static string? RewriteLocation(string location, Uri? upstream, HttpContext context, string prefix)
    {
        string path;
        if (location.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            path = location;
        }
        else if (Uri.TryCreate(location, UriKind.Absolute, out var absolute)
            && absolute.Scheme is "http" or "https"
            && ((upstream is not null && string.Equals(absolute.Authority, upstream.Authority, StringComparison.OrdinalIgnoreCase))
                || string.Equals(absolute.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
            && absolute.AbsolutePath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            path = absolute.PathAndQuery + absolute.Fragment;
        }
        else
        {
            return null;
        }

        return context.Request.PathBase.Value + prefix + path;
    }
}
