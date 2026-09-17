using BookingSystem.Cinema.Application.Contracts.CinemaAccess;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Shared;
using CinemaEntity = BookingSystem.Cinema.Domain.Models.Cinema;

namespace BookingSystem.Cinema.Application.Abstractions.Repositories;

public interface ICinemaRepository : IRepositoryBase<CinemaEntity, Guid>
{
    Task<PagedResult<CinemaListItemDto>> ListAsync(GetCinemasRequest request, CinemaAccessDto scope, CancellationToken cancellationToken);
    Task<CinemaDto> GetCinemaAsync(Guid id, CinemaAccessDto scope, CancellationToken cancellationToken);
    Task<CinemaEntity> GetForUpdateAsync(Guid id, CinemaAccessDto scope, CancellationToken cancellationToken);
    Task<Region> GetRegionAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
}
