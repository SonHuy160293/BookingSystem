using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Films.Commands.UpdateFilm;

public sealed class UpdateFilmCommandValidator : IValidator<UpdateFilmCommand>
{
    public Task ValidateAsync(UpdateFilmCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Request is null)
        {
            throw new CqrsValidationException(["Request is required."]);
        }

        var errors = new List<string>();
        var request = command.Request;
        RequestValidation.Id(errors, command.Id);

        RequestValidation.Text(errors, request.Title, nameof(request.Title), 500, true);
        RequestValidation.Text(errors, request.EnglishTitle, nameof(request.EnglishTitle), 500);
        RequestValidation.Text(errors, request.AgeClassificationCode, nameof(request.AgeClassificationCode), 16);
        if (request.DurationMinutes <= 0)
        {
            errors.Add("DurationMinutes must be positive.");
        }

        RequestValidation.Url(errors, request.PosterUrl, nameof(request.PosterUrl));
        RequestValidation.Url(errors, request.BannerUrl, nameof(request.BannerUrl));
        RequestValidation.Url(errors, request.TrailerUrl, nameof(request.TrailerUrl));
        RequestValidation.Url(errors, request.TrailerVideoUrl, nameof(request.TrailerVideoUrl));
        RequestValidation.Lists(errors, request.CountryCodes, request.FormatTags, request.CaptionOptions, request.GenreIds);

        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
