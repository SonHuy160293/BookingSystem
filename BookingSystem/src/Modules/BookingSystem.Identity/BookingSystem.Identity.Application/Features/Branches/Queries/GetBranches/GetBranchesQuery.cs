using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Branches.Queries.GetBranches;

public sealed record GetBranchesQuery(GetBranchesRequest Request) : IQuery<PagedResult<BranchDto>>;
