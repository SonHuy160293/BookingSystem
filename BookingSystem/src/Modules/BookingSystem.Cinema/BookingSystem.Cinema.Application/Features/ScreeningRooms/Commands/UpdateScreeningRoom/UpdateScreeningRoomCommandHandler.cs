using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.Cinema.Application.Mappings;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.UpdateScreeningRoom;

public sealed class UpdateScreeningRoomCommandHandler : ICommandHandler<UpdateScreeningRoomCommand, ScreeningRoomDto>
{
    private readonly IScreeningRoomRepository _repository;

    public UpdateScreeningRoomCommandHandler(IScreeningRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ScreeningRoomDto>> HandleAsync(UpdateScreeningRoomCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var room = await _repository.GetByIdAsync(command.Id, cancellationToken);
        string code = request.Code.Trim().ToUpperInvariant();
        if (await _repository.CodeExistsAsync(room.CinemaId, code, room.Id, cancellationToken))
        {
            throw new ConflictException("ScreeningRoom.CodeExists", "This screening room code is already in use within the Cinema.");
        }

        room.ChangeDetails(code, request.Name);
        if (request.IsActive)
        {
            room.Activate();
        }
        else
        {
            room.Deactivate();
        }

        return Result.Success(ScreeningRoomMapping.ToDto(room));
    }
}
