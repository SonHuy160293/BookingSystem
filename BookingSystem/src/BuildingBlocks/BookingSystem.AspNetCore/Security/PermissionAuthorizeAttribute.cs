using BookingSystem.SharedKernel.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookingSystem.AspNetCore.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public PermissionAuthorizeAttribute(string function, ActionType action)
    {
        Function = function;
        Action = action;
    }

    public string Function { get; }

    public ActionType Action { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (HasPermission(context))
        {
            return;
        }

        context.Result = context.HttpContext.User.Identity?.IsAuthenticated == true
            ? new ForbidResult()
            : new ChallengeResult();
    }

    private bool HasPermission(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Function))
        {
            return true;
        }

        try
        {
            var requiredPermission = $"{Function}.{Action}";

            return user.HasClaim(CustomClaims.Permission, requiredPermission);
        }
        catch (Exception exception)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PermissionAuthorizeAttribute>>();
            logger.LogError(exception, "Permission authorization failed for {Function}.{Action}.", Function, Action);
            return false;
        }
    }
}
