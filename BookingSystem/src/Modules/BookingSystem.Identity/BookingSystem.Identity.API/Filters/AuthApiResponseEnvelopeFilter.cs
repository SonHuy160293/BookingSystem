using BookingSystem.AspNetCore.Filters;
using BookingSystem.Identity.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingSystem.Identity.API.Filters;

public sealed class AuthApiResponseEnvelopeFilter : IAlwaysRunResultFilter
{
    private readonly ApiResponseEnvelopeFilter _inner = new();
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // The shared envelope translates 204 to 200; preserve the auth endpoint contract.
        if (context.Controller is AuthController && context.Result is StatusCodeResult { StatusCode: StatusCodes.Status204NoContent })
        {
            return;
        }

        _inner.OnResultExecuting(context);
    }

    public void OnResultExecuted(ResultExecutedContext context) => _inner.OnResultExecuted(context);
}
