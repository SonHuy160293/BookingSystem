using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Films.Queries.GetFilmById;

public sealed class GetFilmByIdQueryHandler : IQueryHandler<GetFilmByIdQuery, FilmDto>
{
    private readonly IFilmRepository _repository;
    private readonly ICinemaAccessService _access;

    public GetFilmByIdQueryHandler(IFilmRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<FilmDto>> HandleAsync(GetFilmByIdQuery query, CancellationToken cancellationToken)
    {
        await _access.RequireFilmAccessAsync(query.RequiredAction, cancellationToken);
        return Result.Success(await _repository.GetFilmAsync(query.Id, cancellationToken));
    }
}
