using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.Cinema.Domain.Models;

namespace BookingSystem.Cinema.Application.Mappings;

public static class ScreeningRoomMapping
{
    public static ScreeningRoomDto ToDto(ScreeningRoom room)
        => new(room.Id, room.CinemaId, room.Code, room.Name, room.IsActive, room.CreatedAt, room.UpdatedAt);
}
