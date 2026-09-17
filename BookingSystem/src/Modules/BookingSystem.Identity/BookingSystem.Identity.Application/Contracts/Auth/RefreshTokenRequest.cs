namespace BookingSystem.Identity.Application.Contracts.Auth;

public sealed record RefreshTokenRequest(string RefreshToken, string ClientId);
