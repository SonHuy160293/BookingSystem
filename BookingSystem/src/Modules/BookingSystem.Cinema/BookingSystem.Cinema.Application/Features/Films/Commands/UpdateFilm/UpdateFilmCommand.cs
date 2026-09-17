using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Films.Commands.UpdateFilm;

public sealed record UpdateFilmCommand(Guid Id, UpdateFilmRequest Request) : ICommand<FilmDto>;
