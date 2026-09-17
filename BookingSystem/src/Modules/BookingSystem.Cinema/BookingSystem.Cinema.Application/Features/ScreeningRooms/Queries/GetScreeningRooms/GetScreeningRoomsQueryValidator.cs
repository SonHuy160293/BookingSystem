using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRooms;

public sealed class GetScreeningRoomsQueryValidator : IValidator<GetScreeningRoomsQuery>
{
    public Task ValidateAsync(GetScreeningRoomsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Request is null)
        {
            throw new CqrsValidationException(["Request is required."]);
        }

        var errors = new List<string>();
        var request = query.Request;
        RequestValidation.Paging(errors, request, "Code", "Name", "CreatedAt", "Id");
        if (request.CinemaId == Guid.Empty)
        {
            errors.Add("CinemaId must not be empty when supplied.");
        }

        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
