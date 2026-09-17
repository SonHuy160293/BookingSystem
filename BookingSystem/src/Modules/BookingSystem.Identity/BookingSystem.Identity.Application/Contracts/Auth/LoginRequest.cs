namespace BookingSystem.Identity.Application.Contracts.Auth;

public sealed record LoginRequest(
    string Email,
    string Password,
    string? ClientId = null,
    string? DeviceName = null);
