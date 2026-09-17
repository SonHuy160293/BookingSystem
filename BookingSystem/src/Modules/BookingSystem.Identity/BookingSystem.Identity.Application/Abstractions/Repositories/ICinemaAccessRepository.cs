using BookingSystem.Identity.Application.Contracts.CinemaAccess;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface ICinemaAccessRepository
{
    Task<CinemaAccessDto> GetAccessAsync(Guid userId, string permission, CancellationToken cancellationToken);
    Task<BranchAccessDto> GetBranchAsync(Guid id, CancellationToken cancellationToken);
}
