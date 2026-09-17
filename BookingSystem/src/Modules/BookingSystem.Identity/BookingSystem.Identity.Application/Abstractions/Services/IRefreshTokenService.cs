namespace BookingSystem.Identity.Application.Abstractions.Services;

public interface IRefreshTokenService
{
    string Generate();
    bool TryHash(string? token, out byte[] hash);
}
