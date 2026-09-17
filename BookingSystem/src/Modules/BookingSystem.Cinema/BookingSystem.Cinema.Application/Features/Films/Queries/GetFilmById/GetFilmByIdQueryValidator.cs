using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Films.Queries.GetFilmById;

public sealed class GetFilmByIdQueryValidator : IValidator<GetFilmByIdQuery>
{
    public Task ValidateAsync(GetFilmByIdQuery query, CancellationToken cancellationToken)
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
