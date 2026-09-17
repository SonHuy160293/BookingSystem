using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemaById;

public sealed class GetCinemaByIdQueryValidator : IValidator<GetCinemaByIdQuery>
{
    public Task ValidateAsync(GetCinemaByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<string>();
        RequestValidation.Id(errors, query.Id);
        if (query.RequiredAction != ActionType.View && query.RequiredAction != ActionType.Create && query.RequiredAction != ActionType.Update)
        {
            errors.Add("RequiredAction is not supported.");
        }

        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
