using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Queries.GetSessions;

public sealed record GetSessionsQuery : IQuery<IReadOnlyList<AuthSessionDto>>;
