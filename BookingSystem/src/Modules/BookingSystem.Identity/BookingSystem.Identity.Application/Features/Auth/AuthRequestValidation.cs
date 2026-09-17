using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Security;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Application.Features.Auth;

internal static class AuthRequestValidation
{
    public static void ValidateTokenRequest(RefreshTokenRequest? request, IRefreshTokenService tokens)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ClientId) || request.ClientId.Length > 128 || !tokens.TryHash(request.RefreshToken, out _))
        {
            throw new CqrsValidationException(["A canonical refresh token and client ID of at most 128 characters are required."]);
        }
    }

    public static Guid GetUserId(ICurrentUserProvider provider) => Guid.TryParse(provider.GetCurrentUser()?.Id, out var userId) && userId != Guid.Empty ? userId : throw new UnauthorizedException();
    public static bool CanAuthenticate(BookingSystem.Identity.Domain.Models.User user, DateTime now) => user.IsEnabled && !user.TwoFactorEnabled && !(user.LockoutEnabled && user.LockoutEnd > new DateTimeOffset(now));
}
