using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.API.Middleware;

public sealed class AuthDatabaseFailureMiddleware
{
    private readonly RequestDelegate _next;
    public AuthDatabaseFailureMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthDatabaseException)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Fail(new ApiErrorResponse(
                "Auth.DatabaseUnavailable",
                "Authentication could not be completed. Please sign in again.",
                TraceId: context.TraceIdentifier)),
                context.RequestAborted);
        }
    }
}
