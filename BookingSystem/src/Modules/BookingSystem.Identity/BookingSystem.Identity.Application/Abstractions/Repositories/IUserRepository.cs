using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IUserRepository : IRepositoryBase<User>, IDeletableRepository<User>
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
}
