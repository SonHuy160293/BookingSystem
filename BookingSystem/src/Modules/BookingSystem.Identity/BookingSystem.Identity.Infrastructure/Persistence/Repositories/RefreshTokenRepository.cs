using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Guid?> FindOwnerAsync(byte[] hash, CancellationToken cancellationToken)
        => DbContext.RefreshTokens.AsNoTracking().Where(token => token.TokenHash == hash)
            .Join(
                DbContext.AuthSessions.AsNoTracking(),
                token => token.SessionId,
                session => session.Id,
                (_, session) => (Guid?)session.UserId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<RefreshToken?> FindByHashAsync(byte[] hash, CancellationToken cancellationToken)
        => DbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
}
