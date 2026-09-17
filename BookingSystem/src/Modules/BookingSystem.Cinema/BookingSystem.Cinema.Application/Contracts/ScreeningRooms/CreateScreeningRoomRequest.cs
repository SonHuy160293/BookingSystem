using System.Text.Json.Serialization;

namespace BookingSystem.Cinema.Application.Contracts.ScreeningRooms;

public sealed record CreateScreeningRoomRequest
{
    [JsonRequired]
    public Guid CinemaId { get; init; }
    [JsonRequired]
    public string Code { get; init; } = null!;
    [JsonRequired]
    public string Name { get; init; } = null!;
}
