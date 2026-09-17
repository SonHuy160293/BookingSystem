namespace BookingSystem.Cinema.Application.Contracts.CinemaAccess;

public sealed record CinemaAccessDto(bool HasPlatformAccess, Guid[] TenantIds, BranchAccessDto[] Branches);
