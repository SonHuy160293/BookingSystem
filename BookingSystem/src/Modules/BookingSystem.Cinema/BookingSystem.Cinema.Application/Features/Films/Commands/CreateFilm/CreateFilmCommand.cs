using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Films.Commands.CreateFilm;

public sealed record CreateFilmCommand(CreateFilmRequest Request) : ICommand<FilmDto>;
