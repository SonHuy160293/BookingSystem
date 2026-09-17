namespace BookingSystem.Identity.Application.Contracts.Users;

public sealed record RoleAssignmentRequest(Guid RoleId, Guid? TenantId = null, Guid? BranchId = null);
