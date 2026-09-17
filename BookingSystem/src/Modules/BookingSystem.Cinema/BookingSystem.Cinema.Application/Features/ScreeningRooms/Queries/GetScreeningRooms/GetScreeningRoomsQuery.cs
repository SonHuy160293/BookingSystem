using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Features.ScreeningRooms.Queries.GetScreeningRooms;

public sealed record GetScreeningRoomsQuery(GetScreeningRoomsRequest Request) : IQuery<PagedResult<ScreeningRoomListItemDto>>;
