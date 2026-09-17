using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenantById;

public sealed class GetTenantByIdQueryValidator : IValidator<GetTenantByIdQuery>
{
    public Task ValidateAsync(GetTenantByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (query.Id == Guid.Empty)
        {
            throw new CqrsValidationException(["Id must not be empty."]);
        }

        return Task.CompletedTask;
    }
}
