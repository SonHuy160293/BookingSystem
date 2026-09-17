using BookingSystem.Cinema.Application.Validation;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.CreateScreeningRoom;

public sealed class CreateScreeningRoomCommandValidator : IValidator<CreateScreeningRoomCommand>
{
    public Task ValidateAsync(CreateScreeningRoomCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Request is null)
        {
            throw new CqrsValidationException(["Request is required."]);
        }

        var errors = new List<string>();
        var request = command.Request;
        if (request.CinemaId == Guid.Empty)
        {
            errors.Add("CinemaId must not be empty.");
        }

        RequestValidation.Text(errors, request.Code, nameof(request.Code), 32, true);
        RequestValidation.Text(errors, request.Name, nameof(request.Name), 100, true);
        RequestValidation.ThrowIfInvalid(errors);
        return Task.CompletedTask;
    }
}
