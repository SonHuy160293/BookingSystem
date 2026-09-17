using System.Linq.Expressions;
using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.CinemaAccess;
using BookingSystem.Cinema.Application.Contracts.Cinemas;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.Cinema.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using CinemaEntity = BookingSystem.Cinema.Domain.Models.Cinema;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Repositories;

public sealed class CinemaRepository : RepositoryBase<CinemaEntity>, ICinemaRepository
{
    public CinemaRepository(BookingSystemCinemaDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PagedResult<CinemaListItemDto>> ListAsync(GetCinemasRequest request, CinemaAccessDto scope, CancellationToken cancellationToken)
    {
        var query = ApplyScope(Query(), scope);
        string? search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            string pattern = SearchPattern.Contains(search);
            query = query.Where(cinema => EF.Functions.ILike(cinema.Name, pattern, "\\")
                || EF.Functions.ILike(cinema.Slug, pattern, "\\") || EF.Functions.ILike(cinema.Address, pattern, "\\"));
        }

        if (request.TenantId.HasValue)
        {
            query = query.Where(cinema => cinema.TenantId == request.TenantId);
        }

        if (request.RegionId.HasValue)
        {
            query = query.Where(cinema => cinema.RegionId == request.RegionId);
        }

        if (request.PublicationStatus.HasValue)
        {
            query = query.Where(cinema => cinema.PublicationStatus == request.PublicationStatus);
        }

        if (request.OperatingStatus.HasValue)
        {
            query = query.Where(cinema => cinema.OperatingStatus == request.OperatingStatus);
        }

        if (request.OnlineBookingEnabled.HasValue)
        {
            query = query.Where(cinema => cinema.OnlineBookingEnabled == request.OnlineBookingEnabled);
        }

        int count = await query.CountAsync(cancellationToken);
        int page = request.GetNormalizedPageNumber();
        int size = request.GetNormalizedPageSize();
        if ((long)(page - 1) * size >= count)
        {
            return new PagedResult<CinemaListItemDto>([], page, size, count);
        }

        var items = await Sort(query, request).Skip(request.GetSkip()).Take(size)
            .Select(cinema => new CinemaListItemDto(
                cinema.Id,
                cinema.TenantId,
                cinema.Name,
                cinema.Slug,
                cinema.Address,
                new RegionDto(cinema.Region.Id, cinema.Region.Name, cinema.Region.Slug),
                cinema.PublicationStatus,
                cinema.OperatingStatus,
                cinema.OnlineBookingEnabled,
                cinema.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<CinemaListItemDto>(items, page, size, count);
    }

    public async Task<CinemaDto> GetCinemaAsync(Guid id, CinemaAccessDto scope, CancellationToken cancellationToken)
        => await ApplyScope(Query(cinema => cinema.Id == id), scope)
            .Select(cinema => new CinemaDto(
                cinema.Id,
                cinema.TenantId,
                cinema.Name,
                cinema.Slug,
                cinema.Address,
                cinema.Description,
                cinema.Latitude,
                cinema.Longitude,
                cinema.PublicationStatus,
                cinema.OperatingStatus,
                cinema.OnlineBookingEnabled,
                new RegionDto(cinema.Region.Id, cinema.Region.Name, cinema.Region.Slug),
                cinema.CreatedAt,
                cinema.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Cinema.NotFound", "Cinema was not found.");

    public async Task<CinemaEntity> GetForUpdateAsync(Guid id, CinemaAccessDto scope, CancellationToken cancellationToken)
        => await ApplyScope(TrackedQuery(cinema => cinema.Id == id), scope).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Cinema.NotFound", "Cinema was not found.");

    public async Task<Region> GetRegionAsync(int id, CancellationToken cancellationToken)
        => await DbContext.Regions.AsNoTracking().SingleOrDefaultAsync(region => region.Id == id, cancellationToken)
            ?? throw new CqrsValidationException(["RegionId does not identify an existing Region."]);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => Query(cinema => cinema.Id == id).AnyAsync(cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
        => Query(cinema => cinema.Slug == slug && (!excludingId.HasValue || cinema.Id != excludingId))
            .AnyAsync(cancellationToken);

    private static IQueryable<CinemaEntity> ApplyScope(IQueryable<CinemaEntity> query, CinemaAccessDto scope)
    {
        if (scope.HasPlatformAccess)
        {
            return query;
        }

        Expression<Func<CinemaEntity, bool>> allowed = cinema => scope.TenantIds.Contains(cinema.TenantId);
        var parameter = allowed.Parameters[0];
        Expression body = allowed.Body;
        foreach (var branch in scope.Branches)
        {
            var branchMatch = Expression.AndAlso(
                Expression.Equal(Expression.Property(parameter, nameof(CinemaEntity.Id)), Expression.Constant(branch.Id)),
                Expression.Equal(Expression.Property(parameter, nameof(CinemaEntity.TenantId)), Expression.Constant(branch.TenantId)));
            body = Expression.OrElse(body, branchMatch);
        }

        return query.Where(Expression.Lambda<Func<CinemaEntity, bool>>(body, parameter));
    }

    private static IOrderedQueryable<CinemaEntity> Sort(IQueryable<CinemaEntity> query, GetCinemasRequest request)
    {
        bool descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "CREATEDAT" => descending ? query.OrderByDescending(cinema => cinema.CreatedAt) : query.OrderBy(cinema => cinema.CreatedAt),
            "ID" => descending ? query.OrderByDescending(cinema => cinema.Id) : query.OrderBy(cinema => cinema.Id),
            _ => descending ? query.OrderByDescending(cinema => cinema.Name) : query.OrderBy(cinema => cinema.Name)
        };
        return ordered.ThenBy(cinema => cinema.Id);
    }
}
