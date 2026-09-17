using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaBranch;

public sealed class GetCinemaBranchQueryValidator : IValidator<GetCinemaBranchQuery>
{
    public Task ValidateAsync(GetCinemaBranchQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Id == Guid.Empty)
        {
            throw new CqrsValidationException(["A nonempty identifier is required."]);
        }

        return Task.CompletedTask;
    }
}
