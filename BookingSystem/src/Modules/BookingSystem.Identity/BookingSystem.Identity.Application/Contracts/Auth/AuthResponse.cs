using BookingSystem.Identity.Application.Contracts.Users;

namespace BookingSystem.Identity.Application.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    AccountUserDto User,
    string RefreshToken,
    Guid SessionId,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);
