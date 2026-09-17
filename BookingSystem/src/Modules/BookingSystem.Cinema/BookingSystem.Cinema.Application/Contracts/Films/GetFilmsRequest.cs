using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Contracts.Films;

public sealed record GetFilmsRequest : PagedRequest
{
    public int? GenreId { get; init; }
    public DateOnly? ReleaseDateFrom { get; init; }
    public DateOnly? ReleaseDateTo { get; init; }
    public string? AgeClassificationCode { get; init; }
}
