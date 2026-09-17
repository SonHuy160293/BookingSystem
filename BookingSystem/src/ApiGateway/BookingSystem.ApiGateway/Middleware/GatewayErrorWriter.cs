using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.ApiGateway.Middleware;

public static class GatewayErrorWriter
{
    public static async Task WriteAsync(HttpContext context, int statusCode)
    {
        var (code, message) = Describe(statusCode);
        var state = context.Features.Get<GatewayRequestState>();
        if (state is not null)
        {
            state.ErrorCode = code;
        }

        if (context.Response.HasStarted || state?.ClientDisconnected.IsCancellationRequested == true)
        {
            if (state is not null && !state.ClientDisconnected.IsCancellationRequested)
            {
                state.GatewayAborted = true;
            }

            context.Abort();
            return;
        }

        string allow = context.Response.Headers.Allow.ToString();
        string retryAfter = context.Response.Headers.RetryAfter.ToString();
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }

        if (statusCode == StatusCodes.Status405MethodNotAllowed && allow.Length > 0)
        {
            context.Response.Headers.Allow = allow;
        }

        if (statusCode == StatusCodes.Status429TooManyRequests && retryAfter.Length > 0)
        {
            context.Response.Headers.RetryAfter = retryAfter;
        }

        if (state is not null)
        {
            state.ErrorWritten = true;
        }

        await context.Response.WriteAsJsonAsync(
            ApiResponse.Fail(new ApiErrorResponse(code, message, TraceId: context.TraceIdentifier)),
            cancellationToken: state?.ClientDisconnected ?? CancellationToken.None);
    }

    private static (string Code, string Message) Describe(int statusCode) => statusCode switch
    {
        400 => ("Gateway.BadRequest", "The request is invalid."),
        401 => ("Gateway.Unauthorized", "Authentication is required."),
        403 => ("Gateway.Forbidden", "Access is forbidden."),
        404 => ("Gateway.RouteNotFound", "The gateway route was not found."),
        405 => ("Gateway.MethodNotAllowed", "The HTTP method is not allowed."),
        413 => ("Gateway.PayloadTooLarge", "The request body is too large."),
        429 => ("Gateway.RateLimited", "Too many requests. Please try again later."),
        502 => ("Gateway.BadGateway", "The upstream service could not complete the request."),
        503 => ("Gateway.ServiceUnavailable", "The service is temporarily unavailable."),
        504 => ("Gateway.UpstreamTimeout", "The upstream request timed out."),
        _ => ("Gateway.InternalError", "An unexpected gateway error occurred.")
    };
}
