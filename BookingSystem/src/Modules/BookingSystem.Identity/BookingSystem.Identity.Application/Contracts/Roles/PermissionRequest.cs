namespace BookingSystem.Identity.Application.Contracts.Roles;

public sealed record PermissionRequest(IReadOnlyCollection<string> Permissions);
