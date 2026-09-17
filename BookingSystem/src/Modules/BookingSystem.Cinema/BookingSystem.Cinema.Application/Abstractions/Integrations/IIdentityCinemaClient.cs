using BookingSystem.Cinema.Application.Contracts.CinemaAccess;

namespace BookingSystem.Cinema.Application.Abstractions.Integrations;

public interface IIdentityCinemaClient
{
    Task<CinemaAccessDto> GetAccessAsync(Guid userId, string permission, CancellationToken cancellationToken);
    Task<BranchAccessDto> GetBranchAsync(Guid branchId, CancellationToken cancellationToken);
}
