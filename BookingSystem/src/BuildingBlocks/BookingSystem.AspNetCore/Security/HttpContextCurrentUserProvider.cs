using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BookingSystem.SharedKernel.Security;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.AspNetCore.Security;

public sealed class HttpContextCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser? GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var userName = user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? user.FindFirstValue(ClaimTypes.Name);
        var email = user.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? user.FindFirstValue(ClaimTypes.Email);
        var permissions = user.FindAll(CustomClaims.Permission)
            .Select(claim => claim.Value)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new CurrentUser(userId, userName, email, permissions);
    }
}
