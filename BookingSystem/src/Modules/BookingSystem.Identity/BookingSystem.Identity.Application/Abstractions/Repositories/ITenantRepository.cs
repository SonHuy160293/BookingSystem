using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface ITenantRepository : IRepositoryBase<Tenant, Guid>
{
    Task<PagedResult<TenantDto>> ListAsync(GetTenantsRequest request, CancellationToken cancellationToken);

    Task<TenantDto> GetTenantByIdAsync(Guid id, CancellationToken cancellationToken);
}
