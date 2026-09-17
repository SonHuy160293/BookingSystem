using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return Query(u => u.Email == email).AnyAsync(cancellationToken);
    }
}
