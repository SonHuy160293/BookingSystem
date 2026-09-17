using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.Identity.Domain.Enums;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class RolesRepository : RepositoryBase<Role>, IRolesRepository
{
    public RolesRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PagedResult<RoleDto>> ListAsync(GetRolesRequest request, CancellationToken cancellationToken)
    {
        var query = Query();
        var search = request.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(role =>
                (role.Name != null && role.Name.Contains(search)) ||
                (role.Description != null && role.Description.Contains(search)));
        }

        var pageNumber = request.GetNormalizedPageNumber();
        var pageSize = request.GetNormalizedPageSize();
        var totalCount = await query.CountAsync(cancellationToken);

        // Check the offset before using GetSkip so very large page numbers cannot overflow.
        if ((long)(pageNumber - 1) * pageSize >= totalCount)
        {
            return new PagedResult<RoleDto>([], pageNumber, pageSize, totalCount);
        }

        var roles = await ProjectRoles(ApplySorting(query, request)
                .Skip(request.GetSkip())
                .Take(pageSize))
            .ToListAsync(cancellationToken);
        var items = await AddPermissionsAsync(roles, cancellationToken);

        return new PagedResult<RoleDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<RoleDto> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await ProjectRoles(Query(role => role.Id == id))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Role.NotFound", $"Role '{id}' was not found.");
        var roles = await AddPermissionsAsync([role], cancellationToken);

        return roles.Single();
    }

    public Task<bool> NormalizedNameExistsAsync(string normalizedName, Guid? tenantId, RbacScopeType scopeType, CancellationToken cancellationToken)
        => Query(role => role.NormalizedName == normalizedName && role.TenantId == tenantId && role.ScopeType == scopeType)
            .AnyAsync(cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetEnabledPermissionValuesAsync(IReadOnlyCollection<string> values, CancellationToken cancellationToken)
        => await DbContext.PermissionDefinitions.AsNoTracking()
            .Where(permission => permission.IsEnabled && values.Contains(permission.Value))
            .Select(permission => permission.Value).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetAssignedPermissionValuesAsync(Guid roleId, CancellationToken cancellationToken)
        => await DbContext.RoleClaims.AsNoTracking()
            .Where(claim => claim.RoleId == roleId && claim.ClaimType == CustomClaims.Permission && claim.ClaimValue != null)
            .Select(claim => claim.ClaimValue!).ToListAsync(cancellationToken);

    public async Task AddRoleClaimsAsync(IReadOnlyCollection<RoleClaim> claims, CancellationToken cancellationToken)
        => await DbContext.RoleClaims.AddRangeAsync(claims, cancellationToken);

    private static IQueryable<RoleDto> ProjectRoles(IQueryable<Role> query)
        => query.Select(role => new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt,
            Array.Empty<string>(),
            role.TenantId,
            role.ScopeType == RbacScopeType.Platform ? "PLATFORM" :
                role.ScopeType == RbacScopeType.Tenant ? "TENANT" : "BRANCH"));

    private static IOrderedQueryable<Role> ApplySorting(IQueryable<Role> query, GetRolesRequest request)
    {
        var descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "DESCRIPTION" => descending
                ? query.OrderByDescending(role => role.Description)
                : query.OrderBy(role => role.Description),
            "CREATEDAT" => descending
                ? query.OrderByDescending(role => role.CreatedAt)
                : query.OrderBy(role => role.CreatedAt),
            "ID" => descending
                ? query.OrderByDescending(role => role.Id)
                : query.OrderBy(role => role.Id),
            _ => descending
                ? query.OrderByDescending(role => role.Name)
                : query.OrderBy(role => role.Name)
        };

        return ordered.ThenBy(role => role.Id);
    }

    private async Task<IReadOnlyCollection<RoleDto>> AddPermissionsAsync(
        IReadOnlyCollection<RoleDto> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return roles;
        }

        var roleIds = roles.Select(role => role.Id).ToArray();
        var claims = await DbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => roleIds.Contains(claim.RoleId) && claim.ClaimType == CustomClaims.Permission)
            .Select(claim => new { claim.RoleId, claim.ClaimValue })
            .ToListAsync(cancellationToken);
        var permissions = claims
            .Where(claim => !string.IsNullOrWhiteSpace(claim.ClaimValue))
            .ToLookup(claim => claim.RoleId, claim => claim.ClaimValue!);

        return roles.Select(role => role with
        {
            Permissions = permissions[role.Id]
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray()
        }).ToArray();
    }
}
