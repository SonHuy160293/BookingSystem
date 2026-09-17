using BookingSystem.Identity.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaAccess;

public sealed record GetCinemaAccessQuery(Guid UserId, string Permission) : IQuery<CinemaAccessDto>;
