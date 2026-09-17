using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Films.Queries.GetFilms;

public sealed class GetFilmsQueryHandler : IQueryHandler<GetFilmsQuery, PagedResult<FilmListItemDto>>
{
    private readonly IFilmRepository _repository;
    private readonly ICinemaAccessService _access;

    public GetFilmsQueryHandler(IFilmRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<PagedResult<FilmListItemDto>>> HandleAsync(GetFilmsQuery query, CancellationToken cancellationToken)
    {
        await _access.RequireFilmAccessAsync(ActionType.View, cancellationToken);
        return Result.Success(await _repository.ListAsync(query.Request, cancellationToken));
    }
}
