using BookingSystem.SharedKernel.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Payment.Infrastructure.Persistence.Core;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly BookingSystemPaymentDbContext _dbContext;

    public EfUnitOfWork(BookingSystemPaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken = default)
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
