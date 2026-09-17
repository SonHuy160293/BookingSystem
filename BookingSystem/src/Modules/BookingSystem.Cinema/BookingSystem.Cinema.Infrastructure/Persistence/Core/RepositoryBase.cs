using BookingSystem.EntityFrameworkCore.Repositories;
using BookingSystem.SharedKernel.Abstractions.Domain;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Core;

public abstract class RepositoryBase<TEntity> : EfRepositoryBase<TEntity, Guid, BookingSystemCinemaDbContext>, IRepositoryBase<TEntity, Guid>, IDeletableRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
    protected RepositoryBase(BookingSystemCinemaDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<TEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await FindByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException($"{typeof(TEntity).Name}.NotFound", $"{typeof(TEntity).Name} '{id}' was not found.");

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await AddEntityAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        UpdateEntity(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        RemoveEntity(entity);
    }
}
