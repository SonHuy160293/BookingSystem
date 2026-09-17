using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Tenants.Commands.CreateTenant;

public sealed record CreateTenantCommand(CreateTenantRequest Request) : ICommand<TenantDto>;
