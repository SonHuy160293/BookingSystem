using System.Text.Json.Serialization;
using BookingSystem.Cinema.Domain.Enums;

namespace BookingSystem.Cinema.Application.Contracts.Cinemas;

public sealed record CreateCinemaRequest
{
    [JsonRequired]
    public Guid BranchId { get; init; }
    [JsonRequired]
    public int RegionId { get; init; }
    [JsonRequired]
    public string Name { get; init; } = null!;
    [JsonRequired]
    public string Slug { get; init; } = null!;
    [JsonRequired]
    public string Address { get; init; } = null!;
    public string? Description { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    [JsonRequired]
    public PublicationStatus PublicationStatus { get; init; }
    [JsonRequired]
    public OperatingStatus OperatingStatus { get; init; }
    [JsonRequired]
    public bool OnlineBookingEnabled { get; init; }
}
