using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Commands.CreateCinema;

public sealed record CreateCinemaCommand(CreateCinemaRequest Request) : ICommand<CinemaDto>;
