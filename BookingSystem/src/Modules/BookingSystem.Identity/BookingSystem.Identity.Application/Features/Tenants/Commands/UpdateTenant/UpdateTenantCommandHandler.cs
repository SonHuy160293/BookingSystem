using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.Identity.Application.Mappings;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Tenants.Commands.UpdateTenant;

public sealed class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _repository;

    public UpdateTenantCommandHandler(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantDto>> HandleAsync(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (request.IsActive is not bool isActive)
        {
            throw new CqrsValidationException(["IsActive is required."]);
        }

        var tenant = await _repository.GetByIdAsync(command.Id, cancellationToken);
        tenant.ChangeDetails(
            request.Name, request.LegalName, request.TaxCode, request.ContactEmail, request.ContactPhone, request.Address);

        if (isActive)
        {
            tenant.Activate();
        }
        else
        {
            tenant.Deactivate();
        }

        await _repository.UpdateAsync(tenant, cancellationToken);

        return Result.Success(TenantMapping.ToDto(tenant));
    }
}
