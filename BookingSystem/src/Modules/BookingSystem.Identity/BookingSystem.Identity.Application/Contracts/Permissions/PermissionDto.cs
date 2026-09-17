namespace BookingSystem.Identity.Application.Contracts.Permissions;

public sealed record PermissionDto(string Name, string Value, string GroupName, string? Description, string ScopeType);
