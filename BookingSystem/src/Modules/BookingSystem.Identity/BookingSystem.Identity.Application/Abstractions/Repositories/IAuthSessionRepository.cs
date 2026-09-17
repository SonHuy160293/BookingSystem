using BookingSystem.Identity.Application.Contracts.Auth;
using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IAuthSessionRepository : Persistence.IRepositoryBase<AuthSession>
{
    Task<User?> LockUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<AuthSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthSession>> GetUnrevokedSessionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthSessionDto>> ListActiveAsync(
        Guid userId,
        Guid? currentSessionId,
        DateTime now,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> FindCleanupUsersAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken);
    Task<int> DeleteExpiredFamiliesAsync(
        Guid userId,
        DateTime cutoff,
        int batchSize,
        CancellationToken cancellationToken);
}
