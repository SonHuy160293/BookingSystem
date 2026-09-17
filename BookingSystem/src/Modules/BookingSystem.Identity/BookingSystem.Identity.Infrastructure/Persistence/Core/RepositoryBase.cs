using BookingSystem.EntityFrameworkCore.Repositories;
using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Domain;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Core;

public abstract class RepositoryBase<TEntity> : EfRepositoryBase<TEntity, Guid, BookingSystemIdentityDbContext>, IRepositoryBase<TEntity>, IDeletableRepository<TEntity>
    where TEntity : class, IEntity<Guid>
{
    protected RepositoryBase(BookingSystemIdentityDbContext dbContext)
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
