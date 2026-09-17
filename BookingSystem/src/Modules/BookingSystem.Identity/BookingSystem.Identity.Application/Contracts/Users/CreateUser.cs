namespace BookingSystem.Identity.Application.Contracts.Users;

public sealed record CreateUserRequest(string Name, string Email, string Password);

public sealed record CreateUserResponse(string Id, string Name, string Email);
