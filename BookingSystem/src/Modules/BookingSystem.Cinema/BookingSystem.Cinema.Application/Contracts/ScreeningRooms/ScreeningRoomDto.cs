namespace BookingSystem.Cinema.Application.Contracts.ScreeningRooms;

public sealed record ScreeningRoomDto(
    Guid Id, Guid CinemaId, string Code, string Name, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
