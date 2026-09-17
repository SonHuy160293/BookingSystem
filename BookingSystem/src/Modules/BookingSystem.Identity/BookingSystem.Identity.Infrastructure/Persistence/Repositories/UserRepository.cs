using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    protected IQueryable<User> GetUserByNormalizedEmail(string normalizedEmail) => Query(user => user.NormalizedEmail == normalizedEmail);

    public Task<bool> NormalizedUserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Query(user => user.NormalizedUserName == normalizedUserName).AnyAsync(cancellationToken);

    public Task<bool> NormalizedEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
        => GetUserByNormalizedEmail(normalizedEmail).AnyAsync(cancellationToken);

    public Task<User?> GetUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => GetUserByNormalizedEmail(normalizedEmail).FirstOrDefaultAsync(cancellationToken);

    public async Task<AccountUserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Query(user => user.Id == id)
            .Select(user => new AccountUserDto(
                user.Id,
                user.FullName,
                user.UserName,
                user.Email,
                user.JobTitle,
                user.IsEnabled,
                user.CreatedAt,
                null,
                null))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User.NotFound", $"User '{id}' was not found.");

    public Task<bool> HasRoleAsync(Guid userId, Guid roleId, Guid? tenantId, Guid? branchId, CancellationToken cancellationToken)
        => DbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(
                userRole => userRole.UserId == userId && userRole.RoleId == roleId
                    && userRole.TenantId == tenantId && userRole.BranchId == branchId,
                cancellationToken);

    public async Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken)
        => await DbContext.UserRoles.AddAsync(userRole, cancellationToken);

    public async Task<IReadOnlyList<UserRole>> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken)
        => await DbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Include(userRole => userRole.Role)
            .ThenInclude(role => role.RoleClaims)
            .ToListAsync(cancellationToken);
}
