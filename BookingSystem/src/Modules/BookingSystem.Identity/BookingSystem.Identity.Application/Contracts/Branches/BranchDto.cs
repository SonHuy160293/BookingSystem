namespace BookingSystem.Identity.Application.Contracts.Branches;

public sealed record BranchDto(
    Guid Id,
    string Name,
    string? Address,
    DateTimeOffset CreatedAt,
    Guid TenantId);
