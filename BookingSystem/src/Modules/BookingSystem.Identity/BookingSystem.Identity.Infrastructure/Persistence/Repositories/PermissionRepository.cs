using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Permissions;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository : RepositoryBase<PermissionDefinition>, IPermissionRepository
{
    public PermissionRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PagedResult<PermissionDto>> ListAsync(GetPermissionsRequest request, CancellationToken cancellationToken)
    {
        var query = Query(permission => permission.IsEnabled);
        var scopeType = request.ScopeType?.Trim().ToUpperInvariant();
        var search = request.Search?.Trim();

        if (!string.IsNullOrEmpty(scopeType))
        {
            var parsedScopeType = Enum.Parse<RbacScopeType>(scopeType, true);
            query = query.Where(permission => permission.ScopeType == parsedScopeType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(permission =>
                permission.Name.Contains(search) ||
                permission.Value.Contains(search) ||
                permission.GroupName.Contains(search) ||
                (permission.Description != null && permission.Description.Contains(search)));
        }

        var pageNumber = request.GetNormalizedPageNumber();
        var pageSize = request.GetNormalizedPageSize();
        var totalCount = await query.CountAsync(cancellationToken);

        // Check the offset before GetSkip so large page numbers cannot overflow.
        if ((long)(pageNumber - 1) * pageSize >= totalCount)
        {
            return new PagedResult<PermissionDto>([], pageNumber, pageSize, totalCount);
        }

        var items = await ApplySorting(query, request)
            .Skip(request.GetSkip())
            .Take(pageSize)
            .Select(permission => new PermissionDto(
                permission.Name,
                permission.Value,
                permission.GroupName,
                permission.Description,
                permission.ScopeType == RbacScopeType.Platform ? "PLATFORM" :
                    permission.ScopeType == RbacScopeType.Tenant ? "TENANT" : "BRANCH"))
            .ToListAsync(cancellationToken);

        return new PagedResult<PermissionDto>(items, pageNumber, pageSize, totalCount);
    }

    private static IOrderedQueryable<PermissionDefinition> ApplySorting(IQueryable<PermissionDefinition> query, GetPermissionsRequest request)
    {
        var descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "NAME" => descending
                ? query.OrderByDescending(permission => permission.Name)
                : query.OrderBy(permission => permission.Name),
            "VALUE" => descending
                ? query.OrderByDescending(permission => permission.Value)
                : query.OrderBy(permission => permission.Value),
            "GROUPNAME" => descending
                ? query.OrderByDescending(permission => permission.GroupName)
                : query.OrderBy(permission => permission.GroupName),
            "SCOPETYPE" => descending
                ? query.OrderByDescending(permission => permission.ScopeType)
                : query.OrderBy(permission => permission.ScopeType),
            _ => query.OrderBy(permission => permission.DisplayOrder)
        };

        return ordered.ThenBy(permission => permission.Id);
    }
}
