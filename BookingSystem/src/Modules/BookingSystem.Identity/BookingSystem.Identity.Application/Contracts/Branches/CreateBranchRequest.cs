namespace BookingSystem.Identity.Application.Contracts.Branches;

public sealed record CreateBranchRequest(Guid TenantId, string Name, string? Address = null);
