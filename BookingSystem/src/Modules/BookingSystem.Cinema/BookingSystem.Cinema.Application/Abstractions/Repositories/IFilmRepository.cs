using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Cinema.Application.Abstractions.Repositories;

public interface IFilmRepository : IRepositoryBase<Film, Guid>
{
    Task<PagedResult<FilmListItemDto>> ListAsync(GetFilmsRequest request, CancellationToken cancellationToken);
    Task<FilmDto> GetFilmAsync(Guid id, CancellationToken cancellationToken);
    Task<Film> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Genre>> GetGenresAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
