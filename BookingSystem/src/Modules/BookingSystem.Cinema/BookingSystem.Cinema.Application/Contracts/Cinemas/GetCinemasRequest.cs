using BookingSystem.Cinema.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Contracts.Cinemas;

public sealed record GetCinemasRequest : PagedRequest
{
    public Guid? TenantId { get; init; }
    public int? RegionId { get; init; }
    public PublicationStatus? PublicationStatus { get; init; }
    public OperatingStatus? OperatingStatus { get; init; }
    public bool? OnlineBookingEnabled { get; init; }
}
