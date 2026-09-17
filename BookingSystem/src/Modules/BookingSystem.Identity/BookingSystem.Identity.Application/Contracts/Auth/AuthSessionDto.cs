namespace BookingSystem.Identity.Application.Contracts.Auth;

public sealed record AuthSessionDto(
    Guid SessionId,
    string ClientId,
    string? DeviceName,
    DateTime CreatedAt,
    DateTime LastRefreshedAt,
    DateTime IdleExpiresAt,
    DateTime AbsoluteExpiresAt,
    bool IsCurrent);
