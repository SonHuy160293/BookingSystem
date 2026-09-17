using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Branches.Queries.GetBranchById;

public sealed record GetBranchByIdQuery(Guid Id) : IQuery<BranchDto>;
