using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Tenants.Commands.CreateTenant;

public sealed class CreateTenantCommandValidator : IValidator<CreateTenantCommand>
{
    public Task ValidateAsync(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = command.Request;
        var errors = TenantRequestValidation.ValidateDetails(
            request.Name, request.LegalName, request.TaxCode, request.ContactEmail, request.ContactPhone, request.Address);

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
