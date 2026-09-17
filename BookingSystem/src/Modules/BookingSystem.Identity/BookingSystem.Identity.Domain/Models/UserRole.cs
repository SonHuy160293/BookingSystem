using BookingSystem.Identity.Domain.Enums;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Domain.Models;

public sealed class UserRole
{
    private UserRole() { }

    private UserRole(Guid id, Guid userId, Guid roleId, Guid? tenantId, Guid? branchId)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
        TenantId = tenantId;
        BranchId = branchId;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? BranchId { get; private set; }

    public static UserRole Create(Guid userId, Role role, Guid? tenantId = null, Branch? branch = null)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (userId == Guid.Empty)
        {
            throw new ValidationException([new ValidationError(nameof(UserId), "UserId is required.")]);
        }

        if (role.Id == Guid.Empty)
        {
            throw new ValidationException([new ValidationError(nameof(RoleId), "RoleId is required.")]);
        }

        if (tenantId == Guid.Empty)
        {
            throw new ValidationException(
                [new ValidationError(nameof(TenantId), "TenantId must not be empty when provided.")]);
        }

        if (branch?.Id == Guid.Empty)
        {
            throw new ValidationException([new ValidationError(nameof(BranchId), "BranchId is required.")]);
        }

        switch (role.ScopeType)
        {
            case RbacScopeType.Platform when tenantId is not null || branch is not null:
                throw new ValidationException(
                    [new ValidationError(nameof(TenantId), "PLATFORM assignments cannot have a tenant or branch.")]);
            case RbacScopeType.Tenant when tenantId is null || branch is not null:
                throw new ValidationException(
                    [new ValidationError(nameof(TenantId), "TENANT assignments require a tenant and cannot have a branch.")]);
            case RbacScopeType.Branch when tenantId is null || branch is null:
                throw new ValidationException(
                    [new ValidationError(nameof(BranchId), "BRANCH assignments require both a tenant and a branch.")]);
            case RbacScopeType.Platform:
            case RbacScopeType.Tenant:
            case RbacScopeType.Branch:
                break;
            default:
                throw new ValidationException(
                    [new ValidationError(nameof(RoleId), "Role scope must be PLATFORM, TENANT, or BRANCH.")]);
        }

        if (role.TenantId is not null && role.TenantId != tenantId)
        {
            throw new ValidationException(
                [new ValidationError(nameof(TenantId), "The role must be assigned within its owning tenant.")]);
        }

        if (branch is not null && branch.TenantId != tenantId)
        {
            throw new ValidationException(
                [new ValidationError(nameof(BranchId), "The branch must belong to the assignment tenant.")]);
        }

        return new UserRole(Guid.CreateVersion7(), userId, role.Id, tenantId, branch?.Id);
    }
}
