using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Tenants.Commands.UpdateTenant;

public sealed record UpdateTenantCommand(Guid Id, UpdateTenantRequest Request) : ICommand<TenantDto>;
