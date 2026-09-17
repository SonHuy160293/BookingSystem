using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.UpdateScreeningRoom;

public sealed record UpdateScreeningRoomCommand(Guid Id, UpdateScreeningRoomRequest Request) : ICommand<ScreeningRoomDto>;
