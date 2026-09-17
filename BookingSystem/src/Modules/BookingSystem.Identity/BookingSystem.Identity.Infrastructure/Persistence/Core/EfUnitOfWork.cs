using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Identity.Infrastructure.Persistence.Core;

public sealed class EfUnitOfWork : IUnitOfWork, IAuthUnitOfWork
{
    private readonly BookingSystemIdentityDbContext _dbContext;
    private readonly ILogger<EfUnitOfWork> _logger;

    public EfUnitOfWork(BookingSystemIdentityDbContext dbContext, ILogger<EfUnitOfWork> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
    public async Task<TResponse> ExecuteAuthTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken)
    {
        // An explicit outer strategy also suppresses the provider's nested retry strategy.
        var strategy = new SqlServerRetryingExecutionStrategy(_dbContext, 0, TimeSpan.Zero, null);
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var response = await action(cancellationToken);
                _dbContext.ChangeTracker.DetectChanges();
                var events = _dbContext.ChangeTracker.Entries<AuthSession>()
                    .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                    .Select(entry => (entry.Entity.Id, Reason: entry.State == EntityState.Deleted ? "Cleanup"
                        : entry.Entity.RevokedReason ?? (entry.State == EntityState.Added ? "Login" : "Rotation")))
                    .ToArray();
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                foreach (var securityEvent in events)
                {
                    _logger.LogInformation("Authentication session {SessionId} mutation committed: {Reason}", securityEvent.Id, securityEvent.Reason);
                }

                return response;
            });
        }
        catch (Exception exception) when (exception is SqlException or DbUpdateException or TimeoutException or RetryLimitExceededException)
        {
            // Never attach/log SQL exception text: it can include credential hashes and parameter values.
            _logger.LogError("Authentication transaction failed: {FailureType}", exception.GetType().Name);
            _dbContext.ChangeTracker.Clear();
            throw new AuthDatabaseException();
        }
    }

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var response = await action(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        });
    }
}
