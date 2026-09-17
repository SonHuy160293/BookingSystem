namespace BookingSystem.Identity.Application.Contracts.Users;

public sealed record AccountUserDto(
    Guid Id,
    string? FullName,
    string? UserName,
    string? Email,
    string? JobTitle,
    bool IsEnabled,
    DateTimeOffset CreatedAt);
