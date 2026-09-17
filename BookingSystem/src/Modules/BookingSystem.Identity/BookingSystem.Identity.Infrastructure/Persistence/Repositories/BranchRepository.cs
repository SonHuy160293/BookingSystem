using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Branches;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Identity.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository : RepositoryBase<Branch>, IBranchRepository
{
    public BranchRepository(BookingSystemIdentityDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PagedResult<BranchDto>> ListAsync(GetBranchesRequest request, CancellationToken cancellationToken)
    {
        var query = Query();
        var search = request.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(branch =>
                branch.Name.Contains(search) ||
                (branch.Address != null && branch.Address.Contains(search)));
        }

        var pageNumber = request.GetNormalizedPageNumber();
        var pageSize = request.GetNormalizedPageSize();
        var totalCount = await query.CountAsync(cancellationToken);

        // Check the offset before using GetSkip so very large page numbers cannot overflow.
        if ((long)(pageNumber - 1) * pageSize >= totalCount)
        {
            return new PagedResult<BranchDto>([], pageNumber, pageSize, totalCount);
        }

        var items = await ProjectBranches(ApplySorting(query, request)
                .Skip(request.GetSkip())
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<BranchDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<BranchDto> GetBranchByIdAsync(Guid id, CancellationToken cancellationToken)
        => await ProjectBranches(Query(branch => branch.Id == id))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Branch.NotFound", $"Branch '{id}' was not found.");

    private static IQueryable<BranchDto> ProjectBranches(IQueryable<Branch> query)
        => query.Select(branch => new BranchDto(
            branch.Id,
            branch.Name,
            branch.Address,
            branch.CreatedAt));

    private static IOrderedQueryable<Branch> ApplySorting(IQueryable<Branch> query, GetBranchesRequest request)
    {
        var descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "ADDRESS" => descending
                ? query.OrderByDescending(branch => branch.Address)
                : query.OrderBy(branch => branch.Address),
            "CREATEDAT" => descending
                ? query.OrderByDescending(branch => branch.CreatedAt)
                : query.OrderBy(branch => branch.CreatedAt),
            "ID" => descending
                ? query.OrderByDescending(branch => branch.Id)
                : query.OrderBy(branch => branch.Id),
            _ => descending
                ? query.OrderByDescending(branch => branch.Name)
                : query.OrderBy(branch => branch.Name)
        };

        return ordered.ThenBy(branch => branch.Id);
    }
}
