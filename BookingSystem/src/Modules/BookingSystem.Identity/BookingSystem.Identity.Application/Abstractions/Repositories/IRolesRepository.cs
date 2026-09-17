using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IRolesRepository : IRepositoryBase<Role>
{
    Task<PagedResult<RoleDto>> ListAsync(GetRolesRequest request, CancellationToken cancellationToken);

    Task<RoleDto> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NormalizedNameExistsAsync(string normalizedName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetEnabledPermissionValuesAsync(IReadOnlyCollection<string> values, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetAssignedPermissionValuesAsync(Guid roleId, CancellationToken cancellationToken);
    Task AddRoleClaimsAsync(IReadOnlyCollection<RoleClaim> claims, CancellationToken cancellationToken);
}
