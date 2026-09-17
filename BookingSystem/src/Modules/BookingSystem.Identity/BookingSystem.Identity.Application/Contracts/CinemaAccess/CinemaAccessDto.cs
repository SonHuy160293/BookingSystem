namespace BookingSystem.Identity.Application.Contracts.CinemaAccess;

public sealed record CinemaAccessDto(bool HasPlatformAccess, Guid[] TenantIds, BranchAccessDto[] Branches);
