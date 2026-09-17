namespace BookingSystem.Identity.Application.Contracts.Roles;

public sealed record RoleDto(
    Guid Id,
    string? Name,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Permissions);
