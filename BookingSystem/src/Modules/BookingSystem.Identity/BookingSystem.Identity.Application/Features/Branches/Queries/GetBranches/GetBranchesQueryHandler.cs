using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Branches.Queries.GetBranches;

public sealed class GetBranchesQueryHandler : IQueryHandler<GetBranchesQuery, PagedResult<BranchDto>>
{
    private readonly IBranchRepository _repository;

    public GetBranchesQueryHandler(IBranchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<BranchDto>>> HandleAsync(GetBranchesQuery query, CancellationToken cancellationToken)
    {
        var branches = await _repository.ListAsync(query.Request, cancellationToken);
        return Result.Success(branches);
    }
}
