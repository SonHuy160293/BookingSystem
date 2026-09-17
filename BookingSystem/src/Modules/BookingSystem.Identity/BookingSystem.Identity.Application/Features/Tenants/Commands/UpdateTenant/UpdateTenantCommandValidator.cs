using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Tenants.Commands.UpdateTenant;

public sealed class UpdateTenantCommandValidator : IValidator<UpdateTenantCommand>
{
    public Task ValidateAsync(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = command.Request;
        var errors = TenantRequestValidation.ValidateDetails(
            request.Name, request.LegalName, request.TaxCode, request.ContactEmail, request.ContactPhone, request.Address);

        if (command.Id == Guid.Empty)
        {
            errors.Add("Id must not be empty.");
        }

        if (!request.IsActive.HasValue)
        {
            errors.Add("IsActive is required.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
