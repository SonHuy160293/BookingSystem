using BookingSystem.Cinema.Domain.Enums;

namespace BookingSystem.Cinema.Application.Contracts.Cinemas;

public sealed record CinemaDto(
    Guid Id, Guid TenantId, string Name, string Slug, string Address, string? Description,
    decimal? Latitude, decimal? Longitude, PublicationStatus PublicationStatus, OperatingStatus OperatingStatus,
    bool OnlineBookingEnabled, RegionDto Region, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
