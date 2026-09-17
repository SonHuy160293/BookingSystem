namespace BookingSystem.Identity.Application.Abstractions.Persistence;

public interface IDeletableRepository<TEntity>
    where TEntity : class
{
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken);
}
