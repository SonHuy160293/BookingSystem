using BookingSystem.Cinema.Domain.Enums;

namespace BookingSystem.Cinema.Application.Contracts.Cinemas;

public sealed record CinemaListItemDto(
    Guid Id, Guid TenantId, string Name, string Slug, string Address, RegionDto Region,
    PublicationStatus PublicationStatus, OperatingStatus OperatingStatus, bool OnlineBookingEnabled, DateTimeOffset CreatedAt);
