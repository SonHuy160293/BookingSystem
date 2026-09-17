using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository : Persistence.IRepositoryBase<RefreshToken>
{
    Task<Guid?> FindOwnerAsync(byte[] hash, CancellationToken cancellationToken);
    Task<RefreshToken?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken);
}
