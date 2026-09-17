using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Contracts.ScreeningRooms;

public sealed record GetScreeningRoomsRequest : PagedRequest
{
    public Guid? CinemaId { get; init; }
    public bool? IsActive { get; init; }
}
