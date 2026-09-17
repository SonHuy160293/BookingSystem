using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.SharedKernel.Abstractions.Persistence;

public interface IDeletableRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
{
    Task DeleteByIdAsync(TId id, CancellationToken cancellationToken);
}
