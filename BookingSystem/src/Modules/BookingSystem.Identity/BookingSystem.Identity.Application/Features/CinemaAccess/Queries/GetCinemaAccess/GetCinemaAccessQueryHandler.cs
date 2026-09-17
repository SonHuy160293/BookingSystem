using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.CinemaAccess.Queries.GetCinemaAccess;

public sealed class GetCinemaAccessQueryHandler : IQueryHandler<GetCinemaAccessQuery, CinemaAccessDto>
{
    private readonly ICinemaAccessRepository _repository;

    public GetCinemaAccessQueryHandler(ICinemaAccessRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CinemaAccessDto>> HandleAsync(GetCinemaAccessQuery query, CancellationToken cancellationToken)
        => Result.Success(await _repository.GetAccessAsync(query.UserId, query.Permission, cancellationToken));
}
