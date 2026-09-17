using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Features.Films.Queries.GetFilms;

public sealed record GetFilmsQuery(GetFilmsRequest Request) : IQuery<PagedResult<FilmListItemDto>>;
