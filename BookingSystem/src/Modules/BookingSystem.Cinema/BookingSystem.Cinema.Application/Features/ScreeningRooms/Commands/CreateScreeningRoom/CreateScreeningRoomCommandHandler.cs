using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.Cinema.Application.Mappings;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.CreateScreeningRoom;

public sealed class CreateScreeningRoomCommandHandler : ICommandHandler<CreateScreeningRoomCommand, ScreeningRoomDto>
{
    private readonly IScreeningRoomRepository _repository;
    private readonly ICinemaRepository _cinemas;

    public CreateScreeningRoomCommandHandler(IScreeningRoomRepository repository, ICinemaRepository cinemas)
    {
        _repository = repository;
        _cinemas = cinemas;
    }

    public async Task<Result<ScreeningRoomDto>> HandleAsync(CreateScreeningRoomCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (!await _cinemas.ExistsAsync(request.CinemaId, cancellationToken))
        {
            throw new NotFoundException("Cinema.NotFound", "Cinema was not found.");
        }

        var room = ScreeningRoom.Create(request.CinemaId, request.Code, request.Name);
        if (await _repository.CodeExistsAsync(room.CinemaId, room.Code, null, cancellationToken))
        {
            throw new ConflictException("ScreeningRoom.CodeExists", "This screening room code is already in use within the Cinema.");
        }

        await _repository.AddAsync(room, cancellationToken);
        return Result.Success(ScreeningRoomMapping.ToDto(room));
    }
}
