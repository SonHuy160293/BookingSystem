namespace BookingSystem.Cinema.Application.Contracts.ScreeningRooms;

public sealed record ScreeningRoomListItemDto(
    Guid Id, Guid CinemaId, string Code, string Name, bool IsActive, DateTimeOffset CreatedAt);
