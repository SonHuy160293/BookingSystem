using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Domain.Enums;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Users.Commands.AddUserRole;

public sealed class AddUserRoleCommandHandler : ICommandHandler<AddUserRoleCommand>
{
    private readonly IRolesRepository _rolesRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBranchRepository _branchRepository;

    public AddUserRoleCommandHandler(
        IUserRepository userRepository,
        IRolesRepository rolesRepository,
        IBranchRepository branchRepository)
    {
        _userRepository = userRepository;
        _rolesRepository = rolesRepository;
        _branchRepository = branchRepository;
    }

    public async Task<Result> HandleAsync(AddUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        var role = await _rolesRepository.GetByIdAsync(command.Request.RoleId, cancellationToken);
        var tenantId = command.Request.TenantId;
        var branchId = command.Request.BranchId;
        var validScope = role.ScopeType switch
        {
            RbacScopeType.Platform => !tenantId.HasValue && !branchId.HasValue,
            RbacScopeType.Tenant => tenantId.HasValue && !branchId.HasValue,
            RbacScopeType.Branch => tenantId.HasValue && branchId.HasValue,
            _ => false
        };

        if (!validScope)
        {
            throw new CqrsValidationException(["TenantId and BranchId must match the role's assignment scope."]);
        }

        if (role.TenantId.HasValue && role.TenantId != tenantId)
        {
            throw new CqrsValidationException(["Tenant-owned roles can only be assigned within their owner tenant."]);
        }

        Branch? branch = null;
        if (branchId.HasValue)
        {
            branch = await _branchRepository.GetByIdAsync(branchId.Value, cancellationToken);
            if (branch.TenantId != tenantId)
            {
                throw new CqrsValidationException(["The branch must belong to the assignment tenant."]);
            }
        }

        if (await _userRepository.HasRoleAsync(user.Id, role.Id, tenantId, branchId, cancellationToken))
        {
            return Result.Success();
        }

        await _userRepository.AddUserRoleAsync(UserRole.Create(user.Id, role, tenantId, branch), cancellationToken);

        return Result.Success();
    }
}
