using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Branches.Queries.GetBranchById;

public sealed class GetBranchByIdQueryHandler : IQueryHandler<GetBranchByIdQuery, BranchDto>
{
    private readonly IBranchRepository _repository;

    public GetBranchByIdQueryHandler(IBranchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BranchDto>> HandleAsync(GetBranchByIdQuery query, CancellationToken cancellationToken)
    {
        var branch = await _repository.GetBranchByIdAsync(query.Id, cancellationToken);
        return Result.Success(branch);
    }
}
