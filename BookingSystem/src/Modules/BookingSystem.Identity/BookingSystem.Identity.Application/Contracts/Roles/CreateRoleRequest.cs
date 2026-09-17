namespace BookingSystem.Identity.Application.Contracts.Roles;

public sealed record CreateRoleRequest(string Name, string? Description);
