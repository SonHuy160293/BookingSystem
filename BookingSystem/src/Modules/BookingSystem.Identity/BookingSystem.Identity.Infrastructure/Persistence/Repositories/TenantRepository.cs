using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : RepositoryBase<Tenant>, ITenantRepository
{
    public TenantRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PagedResult<TenantDto>> ListAsync(GetTenantsRequest request, CancellationToken cancellationToken)
    {
        var query = Query();
        var search = request.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(tenant =>
                tenant.Name.Contains(search) ||
                (tenant.LegalName != null && tenant.LegalName.Contains(search)) ||
                (tenant.TaxCode != null && tenant.TaxCode.Contains(search)) ||
                (tenant.ContactEmail != null && tenant.ContactEmail.Contains(search)));
        }

        var pageNumber = request.GetNormalizedPageNumber();
        var pageSize = request.GetNormalizedPageSize();
        var totalCount = await query.CountAsync(cancellationToken);

        // Check the offset before GetSkip so large page numbers cannot overflow.
        if ((long)(pageNumber - 1) * pageSize >= totalCount)
        {
            return new PagedResult<TenantDto>([], pageNumber, pageSize, totalCount);
        }

        var items = await ProjectTenants(ApplySorting(query, request)
                .Skip(request.GetSkip())
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<TenantDto> GetTenantByIdAsync(Guid id, CancellationToken cancellationToken)
        => await ProjectTenants(Query(tenant => tenant.Id == id))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tenant.NotFound", $"Tenant '{id}' was not found.");

    private static IQueryable<TenantDto> ProjectTenants(IQueryable<Tenant> query)
        => query.Select(tenant => new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.LegalName,
            tenant.TaxCode,
            tenant.ContactEmail,
            tenant.ContactPhone,
            tenant.Address,
            tenant.IsActive,
            tenant.CreatedAt,
            tenant.UpdatedAt));

    private static IOrderedQueryable<Tenant> ApplySorting(IQueryable<Tenant> query, GetTenantsRequest request)
    {
        var descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "NAME" => descending
                ? query.OrderByDescending(tenant => tenant.Name)
                : query.OrderBy(tenant => tenant.Name),
            "LEGALNAME" => descending
                ? query.OrderByDescending(tenant => tenant.LegalName)
                : query.OrderBy(tenant => tenant.LegalName),
            "CREATEDAT" => descending
                ? query.OrderByDescending(tenant => tenant.CreatedAt)
                : query.OrderBy(tenant => tenant.CreatedAt),
            "ISACTIVE" => descending
                ? query.OrderByDescending(tenant => tenant.IsActive)
                : query.OrderBy(tenant => tenant.IsActive),
            "ID" => descending
                ? query.OrderByDescending(tenant => tenant.Id)
                : query.OrderBy(tenant => tenant.Id),
            _ => query.OrderBy(tenant => tenant.Name)
        };

        return ordered.ThenBy(tenant => tenant.Id);
    }
}
