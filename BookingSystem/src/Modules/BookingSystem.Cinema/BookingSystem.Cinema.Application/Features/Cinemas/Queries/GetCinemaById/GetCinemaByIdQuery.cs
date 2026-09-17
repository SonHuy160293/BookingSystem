using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemaById;

public sealed record GetCinemaByIdQuery(Guid Id, ActionType RequiredAction = ActionType.View) : IQuery<CinemaDto>;
