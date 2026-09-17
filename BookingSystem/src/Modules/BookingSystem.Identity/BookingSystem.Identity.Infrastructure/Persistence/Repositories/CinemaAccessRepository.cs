using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.CinemaAccess;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class CinemaAccessRepository : ICinemaAccessRepository
{
    private readonly BookingSystemIdentityDbContext _dbContext;

    public CinemaAccessRepository(BookingSystemIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CinemaAccessDto> GetAccessAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var grants = await (
            from assignment in _dbContext.UserRoles.AsNoTracking()
            join user in _dbContext.Users on assignment.UserId equals user.Id
            join role in _dbContext.Roles on assignment.RoleId equals role.Id
            join claim in _dbContext.RoleClaims on role.Id equals claim.RoleId
            join definition in _dbContext.PermissionDefinitions on claim.ClaimValue equals definition.Value
            where user.Id == userId && user.IsEnabled
                && (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now)
                && claim.ClaimType == CustomClaims.Permission && claim.ClaimValue == permission && definition.IsEnabled
                && (role.TenantId == null || role.TenantId == assignment.TenantId)
                && (role.ScopeType == RbacScopeType.Platform
                    || (role.ScopeType == RbacScopeType.Tenant && definition.ScopeType != RbacScopeType.Platform)
                    || (role.ScopeType == RbacScopeType.Branch && definition.ScopeType == RbacScopeType.Branch))
            select new { role.ScopeType, assignment.TenantId, assignment.BranchId })
            .Distinct().ToListAsync(cancellationToken);

        bool platform = grants.Any(grant => grant.ScopeType == RbacScopeType.Platform
            && grant.TenantId == null && grant.BranchId == null);
        var tenantIds = grants.Where(grant => grant.ScopeType == RbacScopeType.Tenant
                && grant.TenantId.HasValue && grant.BranchId == null)
            .Select(grant => grant.TenantId!.Value).Distinct().ToArray();
        var existingTenants = await _dbContext.Tenants.AsNoTracking().Where(tenant => tenantIds.Contains(tenant.Id))
            .Select(tenant => tenant.Id).ToArrayAsync(cancellationToken);
        var branchIds = grants.Where(grant => grant.ScopeType == RbacScopeType.Branch
                && grant.TenantId.HasValue && grant.BranchId.HasValue)
            .Select(grant => grant.BranchId!.Value).Distinct().ToArray();
        var branches = await _dbContext.Branches.AsNoTracking().Where(branch => branchIds.Contains(branch.Id))
            .Select(branch => new BranchAccessDto(branch.Id, branch.TenantId)).ToArrayAsync(cancellationToken);
        var validBranches = branches.Where(branch => grants.Any(grant => grant.ScopeType == RbacScopeType.Branch
            && grant.BranchId == branch.Id && grant.TenantId == branch.TenantId)).ToArray();

        return new CinemaAccessDto(platform, existingTenants, validBranches);
    }

    public async Task<BranchAccessDto> GetBranchAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Branches.AsNoTracking()
            .Where(branch => branch.Id == id && _dbContext.Tenants.Any(tenant => tenant.Id == branch.TenantId))
            .Select(branch => new BranchAccessDto(branch.Id, branch.TenantId))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Branch.NotFound", "Branch was not found.");
}
