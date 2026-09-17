using System.Text.Json.Serialization;

namespace BookingSystem.Cinema.Application.Contracts.ScreeningRooms;

public sealed record UpdateScreeningRoomRequest
{
    [JsonRequired]
    public string Code { get; init; } = null!;
    [JsonRequired]
    public string Name { get; init; } = null!;
    [JsonRequired]
    public bool IsActive { get; init; }
}
