using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.Cinema.Domain.Models;
using CinemaEntity = BookingSystem.Cinema.Domain.Models.Cinema;

namespace BookingSystem.Cinema.Application.Mappings;

public static class CinemaMapping
{
    public static CinemaDto ToDto(CinemaEntity cinema, Region region)
        => new(cinema.Id, cinema.TenantId, cinema.Name, cinema.Slug, cinema.Address, cinema.Description,
            cinema.Latitude, cinema.Longitude, cinema.PublicationStatus, cinema.OperatingStatus,
            cinema.OnlineBookingEnabled, new RegionDto(region.Id, region.Name, region.Slug), cinema.CreatedAt, cinema.UpdatedAt);
}
