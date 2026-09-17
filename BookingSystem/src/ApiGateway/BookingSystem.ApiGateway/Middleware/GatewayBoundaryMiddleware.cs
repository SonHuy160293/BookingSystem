using System.Diagnostics;
using BookingSystem.ApiGateway.Logging;
using BookingSystem.ApiGateway.Routing;
using Yarp.ReverseProxy.Forwarder;

namespace BookingSystem.ApiGateway.Middleware;

public sealed class GatewayBoundaryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayBoundaryMiddleware> _logger;

    public GatewayBoundaryMiddleware(RequestDelegate next, ILogger<GatewayBoundaryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var state = new GatewayRequestState { ClientDisconnected = context.RequestAborted };
        context.Features.Set(state);
        using var disconnectRegistration = state.ClientDisconnected.Register(() =>
        {
            // YARP reports a copy failure before aborting a partial response.
            // A genuine client disconnect cancels this token before that report.
            if (context.Features.Get<IForwarderErrorFeature>() is { Error: not ForwarderError.None })
            {
                state.GatewayAborted = true;
            }
        });
        context.Request.Headers["X-Correlation-Id"] = context.TraceIdentifier;
        context.Request.Headers.Remove("baggage");
        if (Activity.Current is { } activity)
        {
            foreach (var baggage in activity.Baggage.ToArray())
            {
                activity.SetBaggage(baggage.Key, null);
            }
        }

        long started = Stopwatch.GetTimestamp();
        try
        {
            await _next(context);
            var error = context.Features.Get<IForwarderErrorFeature>();
            if (error is not null && error.Error != ForwarderError.None && !state.ErrorWritten)
            {
                state.ForwarderErrorCode = error.Error.ToString();
                int status = error.Error switch
                {
                    ForwarderError.NoAvailableDestinations => StatusCodes.Status503ServiceUnavailable,
                    ForwarderError.RequestTimedOut or ForwarderError.UpgradeActivityTimeout => StatusCodes.Status504GatewayTimeout,
                    ForwarderError.RequestCanceled or ForwarderError.RequestBodyCanceled
                        or ForwarderError.ResponseBodyCanceled or ForwarderError.UpgradeRequestCanceled
                        or ForwarderError.UpgradeResponseCanceled => StatusCodes.Status504GatewayTimeout,
                    _ => ContainsTimeout(error.Exception) ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status502BadGateway
                };
                if (FindBadRequest(error.Exception) is { } badRequest)
                {
                    status = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                        ? StatusCodes.Status413PayloadTooLarge : StatusCodes.Status400BadRequest;
                }

                await GatewayErrorWriter.WriteAsync(context, status);
            }
            else if (!state.UpstreamResponseReceived && !state.ErrorWritten && context.Response.StatusCode >= 400)
            {
                await GatewayErrorWriter.WriteAsync(context, context.Response.StatusCode);
            }
        }
        catch (BadHttpRequestException exception)
        {
            await GatewayErrorWriter.WriteAsync(context, exception.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? StatusCodes.Status413PayloadTooLarge : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            await GatewayErrorWriter.WriteAsync(context, StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception)
        {
            await GatewayErrorWriter.WriteAsync(context, StatusCodes.Status500InternalServerError);
        }
        finally
        {
            int status = context.Response.StatusCode;
            if (state.ClientDisconnected.IsCancellationRequested && !state.GatewayAborted)
            {
                state.ErrorCode = "Gateway.ClientDisconnected";
                status = 499;
            }

            var endpoint = GatewayEndpointMetadata.From(context);
            string method = context.Request.Method;
            if (method is not ("GET" or "HEAD" or "POST" or "PUT" or "PATCH" or "DELETE" or "OPTIONS"))
            {
                method = "OTHER";
            }

            _logger.LogInformation(GatewayLogging.RequestTemplate,
                method, endpoint.RouteId, endpoint.ClusterId, status,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                state.ErrorCode, state.ForwarderErrorCode,
                Activity.Current?.TraceId.ToString() ?? "None",
                Activity.Current?.SpanId.ToString() ?? "None");
        }
    }

    private static bool ContainsTimeout(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }
        }

        return false;
    }

    private static BadHttpRequestException? FindBadRequest(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is BadHttpRequestException badRequest)
            {
                return badRequest;
            }
        }

        return null;
    }
}
