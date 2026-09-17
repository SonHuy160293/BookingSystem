using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRoomById;

public sealed class GetScreeningRoomByIdQueryValidator : IValidator<GetScreeningRoomByIdQuery>
{
    public Task ValidateAsync(GetScreeningRoomByIdQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<string>();
        RequestValidation.Id(errors, query.Id);
        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
