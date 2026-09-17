using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class AuthSessionRepository : RepositoryBase<AuthSession>, IAuthSessionRepository
{
    public AuthSessionRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<User?> LockUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (DbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Authentication locks require a command transaction.");
        }

        // Deleted accounts still need their families revoked and cleaned.
        return await DbContext.Users.FromSqlInterpolated($"SELECT * FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}").IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => DbContext.Users.AsNoTracking().Where(user => user.NormalizedEmail == normalizedEmail).Select(user => (Guid?)user.Id).FirstOrDefaultAsync(cancellationToken);
    public Task<AuthSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken) => DbContext.AuthSessions.SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    public async Task<IReadOnlyList<AuthSession>> GetUnrevokedSessionsAsync(Guid userId, CancellationToken cancellationToken) => await DbContext.AuthSessions.Where(session => session.UserId == userId && session.RevokedAt == null).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<AuthSessionDto>> ListActiveAsync(
        Guid userId,
        Guid? currentSessionId,
        DateTime now,
        CancellationToken cancellationToken) => await DbContext.AuthSessions.AsNoTracking().Where(session => session.UserId == userId && session.RevokedAt == null && session.IdleExpiresAt > now && session.AbsoluteExpiresAt > now).OrderByDescending(session => session.LastRefreshedAt).ThenBy(session => session.Id).Select(session => new AuthSessionDto(
        session.Id,
        session.ClientId,
        session.DeviceName,
        session.CreatedAt,
        session.LastRefreshedAt,
        session.IdleExpiresAt,
        session.AbsoluteExpiresAt,
        session.Id == currentSessionId)).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Guid>> FindCleanupUsersAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken) => await DbContext.AuthSessions.AsNoTracking().Where(session => session.AbsoluteExpiresAt <= cutoff).Select(session => session.UserId).Distinct().Order().Take(batchSize).ToListAsync(cancellationToken);
    public async Task<int> DeleteExpiredFamiliesAsync(
        Guid userId,
        DateTime cutoff,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var sessions = await DbContext.AuthSessions.Where(session => session.UserId == userId && session.AbsoluteExpiresAt <= cutoff).OrderBy(session => session.AbsoluteExpiresAt).ThenBy(session => session.Id).Take(batchSize).ToListAsync(cancellationToken);
        var ids = sessions.Select(session => session.Id).ToArray();

        // Limit token rows too: a terminal family can contain many historical rotations.
        var tokens = await DbContext.RefreshTokens.Where(token => ids.Contains(token.SessionId)).OrderBy(token => token.Id).Take(1000).ToListAsync(cancellationToken);
        var tokenCounts = await DbContext.RefreshTokens.Where(token => ids.Contains(token.SessionId)).GroupBy(token => token.SessionId).Select(group => new { SessionId = group.Key, Count = group.Count() }).ToDictionaryAsync(item => item.SessionId, item => item.Count, cancellationToken);
        var completeFamilies = sessions.Where(session => !tokenCounts.TryGetValue(session.Id, out int count) || tokens.Count(token => token.SessionId == session.Id) == count).ToArray();
        DbContext.RefreshTokens.RemoveRange(tokens);
        DbContext.AuthSessions.RemoveRange(completeFamilies);
        return completeFamilies.Length;
    }
}
