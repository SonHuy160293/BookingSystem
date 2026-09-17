using System.IdentityModel.Tokens.Jwt;
using BookingSystem.ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BookingSystem.ApiGateway.Security;

public sealed class GatewayJwtBearerEvents : JwtBearerEvents
{
    public override Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        return GatewayErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized);
    }

    public override Task Forbidden(ForbiddenContext context)
        => GatewayErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden);

    public override Task TokenValidated(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
            || userId == Guid.Empty)
        {
            context.Fail("Invalid user identifier.");
        }

        return Task.CompletedTask;
    }
}
