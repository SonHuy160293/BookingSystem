using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Persistence;

namespace BookingSystem.Identity.Application.Abstractions.Repositories;

public interface IUserRepository : IRepositoryBase<User>
{
    Task<bool> NormalizedUserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken);

    Task<bool> NormalizedEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<AccountUserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasRoleAsync(Guid userId, Guid roleId, Guid? tenantId, Guid? branchId, CancellationToken cancellationToken);

    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken);
}
