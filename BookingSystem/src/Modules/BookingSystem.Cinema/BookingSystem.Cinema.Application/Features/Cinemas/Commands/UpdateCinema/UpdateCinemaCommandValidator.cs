using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Commands.UpdateCinema;

public sealed class UpdateCinemaCommandValidator : IValidator<UpdateCinemaCommand>
{
    public Task ValidateAsync(UpdateCinemaCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Request is null)
        {
            throw new CqrsValidationException(["Request is required."]);
        }

        var errors = new List<string>();
        var request = command.Request;
        RequestValidation.Id(errors, command.Id);

        RequestValidation.Text(errors, request.Name, nameof(request.Name), 250, true);
        RequestValidation.Text(errors, request.Slug, nameof(request.Slug), 250, true);
        RequestValidation.Text(errors, request.Address, nameof(request.Address), 1000, true);
        string slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (slug.Length > 0 && (!char.IsLetterOrDigit(slug[0]) || !char.IsLetterOrDigit(slug[^1])
            || slug.Any(character => !char.IsLetterOrDigit(character) && character != '-')))
        {
            errors.Add("Slug must contain letters, digits or hyphens and begin and end with a letter or digit.");
        }

        if (request.RegionId <= 0)
        {
            errors.Add("RegionId must be positive.");
        }

        if (!Enum.IsDefined(request.PublicationStatus) || !Enum.IsDefined(request.OperatingStatus))
        {
            errors.Add("PublicationStatus and OperatingStatus must be valid enum values.");
        }

        if (request.Latitude.HasValue != request.Longitude.HasValue || request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            errors.Add("Coordinates must be supplied together and be within valid latitude/longitude ranges.");
        }

        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
