using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemas;

public sealed class GetCinemasQueryHandler : IQueryHandler<GetCinemasQuery, PagedResult<CinemaListItemDto>>
{
    private readonly ICinemaRepository _repository;
    private readonly ICinemaAccessService _access;

    public GetCinemasQueryHandler(ICinemaRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<PagedResult<CinemaListItemDto>>> HandleAsync(GetCinemasQuery query, CancellationToken cancellationToken)
    {
        var scope = await _access.GetCinemaScopeAsync(ActionType.View, cancellationToken);
        return Result.Success(await _repository.ListAsync(query.Request, scope, cancellationToken));
    }
}
