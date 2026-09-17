using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaBranch;

public sealed class GetCinemaBranchQueryHandler : IQueryHandler<GetCinemaBranchQuery, BranchAccessDto>
{
    private readonly ICinemaAccessRepository _repository;

    public GetCinemaBranchQueryHandler(ICinemaAccessRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BranchAccessDto>> HandleAsync(GetCinemaBranchQuery query, CancellationToken cancellationToken)
        => Result.Success(await _repository.GetBranchAsync(query.Id, cancellationToken));
}
