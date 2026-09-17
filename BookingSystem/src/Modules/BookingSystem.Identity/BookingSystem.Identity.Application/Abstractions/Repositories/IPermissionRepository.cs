using BookingSystem.Identity.Application.Contracts.Permissions;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IPermissionRepository : IRepositoryBase<PermissionDefinition, Guid>
{
    Task<PagedResult<PermissionDto>> ListAsync(GetPermissionsRequest request, CancellationToken cancellationToken);
}
