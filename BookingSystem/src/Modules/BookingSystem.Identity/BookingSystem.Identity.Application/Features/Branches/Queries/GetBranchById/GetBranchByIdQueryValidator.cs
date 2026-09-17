using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Branches.Queries.GetBranchById;

public sealed class GetBranchByIdQueryValidator : IValidator<GetBranchByIdQuery>
{
    public Task ValidateAsync(GetBranchByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (query.Id == Guid.Empty)
        {
            throw new CqrsValidationException(["Id must not be empty."]);
        }

        return Task.CompletedTask;
    }
}
