using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRoomById;

public sealed record GetScreeningRoomByIdQuery(Guid Id) : IQuery<ScreeningRoomDto>;
