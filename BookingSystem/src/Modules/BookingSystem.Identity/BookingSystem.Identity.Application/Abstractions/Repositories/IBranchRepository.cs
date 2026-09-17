using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IBranchRepository : IRepositoryBase<Branch>
{
    Task<PagedResult<BranchDto>> ListAsync(GetBranchesRequest request, CancellationToken cancellationToken);

    Task<BranchDto> GetBranchByIdAsync(Guid id, CancellationToken cancellationToken);
}
