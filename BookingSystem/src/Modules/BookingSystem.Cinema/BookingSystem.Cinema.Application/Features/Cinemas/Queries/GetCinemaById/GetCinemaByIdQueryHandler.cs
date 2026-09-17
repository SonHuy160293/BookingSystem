using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Abstractions.Security;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Cinema.Application.Features.Cinemas.Queries.GetCinemaById;

public sealed class GetCinemaByIdQueryHandler : IQueryHandler<GetCinemaByIdQuery, CinemaDto>
{
    private readonly ICinemaRepository _repository;
    private readonly ICinemaAccessService _access;

    public GetCinemaByIdQueryHandler(ICinemaRepository repository, ICinemaAccessService access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<Result<CinemaDto>> HandleAsync(GetCinemaByIdQuery query, CancellationToken cancellationToken)
    {
        var scope = await _access.GetCinemaScopeAsync(query.RequiredAction, cancellationToken);
        return Result.Success(await _repository.GetCinemaAsync(query.Id, scope, cancellationToken));
    }
}
