using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemas;

public sealed class GetCinemasQueryValidator : IValidator<GetCinemasQuery>
{
    public Task ValidateAsync(GetCinemasQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<string>();
        if (query.Request is null)
        {
            throw new CqrsValidationException(["Request is required."]);
        }

        var request = query.Request;
        RequestValidation.Paging(errors, request, "Name", "CreatedAt", "Id");
        if (request.TenantId == Guid.Empty || request.RegionId is <= 0)
        {
            errors.Add("TenantId and RegionId must be valid identifiers when supplied.");
        }

        if ((request.PublicationStatus.HasValue && !Enum.IsDefined(request.PublicationStatus.Value))
            || (request.OperatingStatus.HasValue && !Enum.IsDefined(request.OperatingStatus.Value)))
        {
            errors.Add("Status filters must be valid enum values.");
        }

        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
