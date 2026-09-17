using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Commands.CreateScreeningRoom;

public sealed record CreateScreeningRoomCommand(CreateScreeningRoomRequest Request) : ICommand<ScreeningRoomDto>;
