using BookingSystem.Identity.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaBranch;

public sealed record GetCinemaBranchQuery(Guid Id) : IQuery<BranchAccessDto>;
