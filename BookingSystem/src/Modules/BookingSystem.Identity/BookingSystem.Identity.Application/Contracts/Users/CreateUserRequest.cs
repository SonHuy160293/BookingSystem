namespace BookingSystem.Identity.Application.Contracts.Users;

public sealed record CreateUserRequest(
    string FullName,
    string UserName,
    string Email,
    string Password,
    string JobTitle,
    bool IsEnabled);
