using BookingSystem.Cinema.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Abstractions.Security;

public interface ICinemaAccessService
{
    Task<CinemaAccessDto> GetCinemaScopeAsync(ActionType action, CancellationToken cancellationToken);
    Task RequireFilmAccessAsync(ActionType action, CancellationToken cancellationToken);
    Task<BranchAccessDto> RequireBranchAsync(Guid branchId, CinemaAccessDto scope, CancellationToken cancellationToken);
}
