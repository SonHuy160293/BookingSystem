using BookingSystem.ApiGateway.Configuration;
using BookingSystem.ApiGateway.Middleware;
using Microsoft.Extensions.Options;

namespace BookingSystem.ApiGateway.Security;

public sealed class GatewaySecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayOptions _options;

    public GatewaySecurityMiddleware(RequestDelegate next, IOptions<GatewayOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });

        if (!_options.AllowedHosts.Contains(context.Request.Host.Host.Trim('[', ']'), StringComparer.OrdinalIgnoreCase))
        {
            await GatewayErrorWriter.WriteAsync(context, StatusCodes.Status400BadRequest);
            return;
        }

        if (context.Request.ContentLength > _options.MaxRequestBodyBytes)
        {
            await GatewayErrorWriter.WriteAsync(context, StatusCodes.Status413PayloadTooLarge);
            return;
        }

        var originalBody = context.Request.Body;
        using var limitedBody = new GatewayRequestBodyStream(originalBody, _options.MaxRequestBodyBytes);
        context.Request.Body = limitedBody;
        try
        {
            await _next(context);
        }
        finally
        {
            // The server retains ownership of its original stream.
            context.Request.Body = originalBody;
        }
    }
}
