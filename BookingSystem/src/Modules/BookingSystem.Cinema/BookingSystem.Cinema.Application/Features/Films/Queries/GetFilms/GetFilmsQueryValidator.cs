using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Films.Queries.GetFilms;

public sealed class GetFilmsQueryValidator : IValidator<GetFilmsQuery>
{
    public Task ValidateAsync(GetFilmsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<string>();
        if (query.Request is null)
        {
            throw new CqrsValidationException(["Request is required."]);
        }

        var request = query.Request;
        RequestValidation.Paging(errors, request, "Title", "ReleaseDate", "DurationMinutes", "CreatedAt", "Id");
        if (request.GenreId is <= 0)
        {
            errors.Add("GenreId must be positive.");
        }

        if (request.ReleaseDateFrom > request.ReleaseDateTo)
        {
            errors.Add("ReleaseDateFrom must not exceed ReleaseDateTo.");
        }

        RequestValidation.Text(errors, request.AgeClassificationCode, nameof(request.AgeClassificationCode), 16);
        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
