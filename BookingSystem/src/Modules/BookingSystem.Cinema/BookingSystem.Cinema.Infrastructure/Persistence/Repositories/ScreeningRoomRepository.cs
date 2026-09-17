using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.ScreeningRooms;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.Cinema.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Repositories;

public sealed class ScreeningRoomRepository : RepositoryBase<ScreeningRoom>, IScreeningRoomRepository
{
    public ScreeningRoomRepository(BookingSystemCinemaDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<ScreeningRoomDto> GetScreeningRoomAsync(Guid id, CancellationToken cancellationToken)
        => await Query(room => room.Id == id)
            .Select(room => new ScreeningRoomDto(
                room.Id, room.CinemaId, room.Code, room.Name, room.IsActive, room.CreatedAt, room.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("ScreeningRoom.NotFound", "Screening room was not found.");

    public async Task<PagedResult<ScreeningRoomListItemDto>> ListAsync(GetScreeningRoomsRequest request, CancellationToken cancellationToken)
    {
        var query = Query();
        string? search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            string pattern = SearchPattern.Contains(search);
            query = query.Where(room => EF.Functions.ILike(room.Code, pattern, "\\")
                || EF.Functions.ILike(room.Name, pattern, "\\"));
        }

        if (request.CinemaId.HasValue)
        {
            query = query.Where(room => room.CinemaId == request.CinemaId);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(room => room.IsActive == request.IsActive);
        }

        int count = await query.CountAsync(cancellationToken);
        int page = request.GetNormalizedPageNumber();
        int size = request.GetNormalizedPageSize();
        if ((long)(page - 1) * size >= count)
        {
            return new PagedResult<ScreeningRoomListItemDto>([], page, size, count);
        }

        var items = await Sort(query, request).Skip(request.GetSkip()).Take(size)
            .Select(room => new ScreeningRoomListItemDto(
                room.Id, room.CinemaId, room.Code, room.Name, room.IsActive, room.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<ScreeningRoomListItemDto>(items, page, size, count);
    }

    public Task<bool> CodeExistsAsync(Guid cinemaId, string normalizedCode, Guid? excludingId, CancellationToken cancellationToken)
        => Query(room => room.CinemaId == cinemaId && room.Code == normalizedCode
            && (!excludingId.HasValue || room.Id != excludingId))
            .AnyAsync(cancellationToken);

    private static IOrderedQueryable<ScreeningRoom> Sort(IQueryable<ScreeningRoom> query, GetScreeningRoomsRequest request)
    {
        bool descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "NAME" => descending ? query.OrderByDescending(room => room.Name) : query.OrderBy(room => room.Name),
            "CREATEDAT" => descending ? query.OrderByDescending(room => room.CreatedAt) : query.OrderBy(room => room.CreatedAt),
            "ID" => descending ? query.OrderByDescending(room => room.Id) : query.OrderBy(room => room.Id),
            _ => descending ? query.OrderByDescending(room => room.Code) : query.OrderBy(room => room.Code)
        };
        return ordered.ThenBy(room => room.Id);
    }
}
