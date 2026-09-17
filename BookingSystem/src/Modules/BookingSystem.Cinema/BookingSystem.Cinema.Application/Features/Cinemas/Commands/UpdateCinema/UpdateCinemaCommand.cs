using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Commands.UpdateCinema;

public sealed record UpdateCinemaCommand(Guid Id, UpdateCinemaRequest Request) : ICommand<CinemaDto>;
