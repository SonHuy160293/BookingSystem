using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemas;

public sealed record GetCinemasQuery(GetCinemasRequest Request) : IQuery<PagedResult<CinemaListItemDto>>;
