using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Films.Queries.GetFilmById;

public sealed record GetFilmByIdQuery(Guid Id, ActionType RequiredAction = ActionType.View) : IQuery<FilmDto>;
