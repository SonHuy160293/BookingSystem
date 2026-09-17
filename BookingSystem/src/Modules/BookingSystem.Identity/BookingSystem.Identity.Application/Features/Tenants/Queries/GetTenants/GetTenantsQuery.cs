using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenants;

public sealed record GetTenantsQuery(GetTenantsRequest Request) : IQuery<PagedResult<TenantDto>>;
