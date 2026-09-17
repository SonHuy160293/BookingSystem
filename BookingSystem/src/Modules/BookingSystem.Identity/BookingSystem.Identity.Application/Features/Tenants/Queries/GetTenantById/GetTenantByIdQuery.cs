using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenantById;

public sealed record GetTenantByIdQuery(Guid Id) : IQuery<TenantDto>;
