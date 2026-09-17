namespace BookingSystem.Identity.Application.Abstractions.Persistence;

public interface IAuthUnitOfWork
{
    Task<TResponse> ExecuteAuthTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken);
}
