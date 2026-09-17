using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.Identity.Application.Mappings;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Tenants.Commands.CreateTenant;

public sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _repository;

    public CreateTenantCommandHandler(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantDto>> HandleAsync(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var tenant = Tenant.Create(
            request.Name, request.LegalName, request.TaxCode, request.ContactEmail, request.ContactPhone, request.Address);
        await _repository.AddAsync(tenant, cancellationToken);

        return Result.Success(TenantMapping.ToDto(tenant));
    }
}
