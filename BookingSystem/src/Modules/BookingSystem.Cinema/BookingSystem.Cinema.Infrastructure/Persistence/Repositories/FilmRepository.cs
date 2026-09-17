using BookingSystem.Cinema.Application.Abstractions.Repositories;
using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.Cinema.Domain.Models;
using BookingSystem.Cinema.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Cinema.Infrastructure.Persistence.Repositories;

public sealed class FilmRepository : RepositoryBase<Film>, IFilmRepository
{
    public FilmRepository(BookingSystemCinemaDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PagedResult<FilmListItemDto>> ListAsync(GetFilmsRequest request, CancellationToken cancellationToken)
    {
        var query = Query();
        string? search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            string pattern = SearchPattern.Contains(search);
            query = query.Where(film => EF.Functions.ILike(film.Title, pattern, "\\")
                || (film.EnglishTitle != null && EF.Functions.ILike(film.EnglishTitle, pattern, "\\")));
        }

        if (request.GenreId.HasValue)
        {
            query = query.Where(film => film.Genres.Any(membership => membership.GenreId == request.GenreId));
        }

        if (request.ReleaseDateFrom.HasValue)
        {
            query = query.Where(film => film.ReleaseDate >= request.ReleaseDateFrom);
        }

        if (request.ReleaseDateTo.HasValue)
        {
            query = query.Where(film => film.ReleaseDate <= request.ReleaseDateTo);
        }

        if (!string.IsNullOrWhiteSpace(request.AgeClassificationCode))
        {
            string code = request.AgeClassificationCode.Trim();
            query = query.Where(film => film.AgeClassificationCode == code);
        }

        int count = await query.CountAsync(cancellationToken);
        int page = request.GetNormalizedPageNumber();
        int size = request.GetNormalizedPageSize();
        if ((long)(page - 1) * size >= count)
        {
            return new PagedResult<FilmListItemDto>([], page, size, count);
        }

        var items = await Sort(query, request).Skip(request.GetSkip()).Take(size)
            .Select(film => new FilmListItemDto(
                film.Id,
                film.Title,
                film.EnglishTitle,
                film.DurationMinutes,
                film.ReleaseDate,
                film.AgeClassificationCode,
                film.PosterUrl,
                film.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<FilmListItemDto>(items, page, size, count);
    }

    public async Task<FilmDto> GetFilmAsync(Guid id, CancellationToken cancellationToken)
        => await Query(film => film.Id == id)
            .Select(film => new FilmDto(
                film.Id,
                film.Title,
                film.EnglishTitle,
                film.Synopsis,
                film.EnglishSynopsis,
                film.DurationMinutes,
                film.ReleaseDate,
                film.AgeClassificationCode,
                film.PosterUrl,
                film.BannerUrl,
                film.TrailerUrl,
                film.TrailerVideoUrl,
                EF.Property<string[]>(film, "_countryCodes"),
                EF.Property<string[]>(film, "_formatTags"),
                EF.Property<string[]>(film, "_captionOptions"),
                film.Genres.OrderBy(membership => membership.GenreId)
                    .Select(membership => new GenreDto(membership.GenreId, membership.Genre.Name, membership.Genre.Slug)).ToArray(),
                film.CreatedAt,
                film.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Film.NotFound", "Film was not found.");

    public async Task<Film> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await TrackedQuery(film => film.Id == id).Include(film => film.Genres).ThenInclude(membership => membership.Genre)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Film.NotFound", "Film was not found.");

    public override Task UpdateAsync(Film entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Audit the aggregate even for genre-only changes, without marking lookup Genres modified.
        DbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<Genre>> GetGenresAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var genres = await DbContext.Genres.AsTracking().Where(genre => ids.Contains(genre.Id)).ToListAsync(cancellationToken);
        if (genres.Count != ids.Distinct().Count())
        {
            throw new CqrsValidationException(["One or more GenreIds do not identify existing Genres."]);
        }

        return genres;
    }

    private static IOrderedQueryable<Film> Sort(IQueryable<Film> query, GetFilmsRequest request)
    {
        bool descending = request.IsSortDescending();
        var ordered = request.SortBy?.ToUpperInvariant() switch
        {
            "RELEASEDATE" => descending
                ? query.OrderBy(film => film.ReleaseDate == null).ThenByDescending(film => film.ReleaseDate)
                : query.OrderBy(film => film.ReleaseDate == null).ThenBy(film => film.ReleaseDate),
            "DURATIONMINUTES" => descending ? query.OrderByDescending(film => film.DurationMinutes) : query.OrderBy(film => film.DurationMinutes),
            "CREATEDAT" => descending ? query.OrderByDescending(film => film.CreatedAt) : query.OrderBy(film => film.CreatedAt),
            "ID" => descending ? query.OrderByDescending(film => film.Id) : query.OrderBy(film => film.Id),
            _ => descending ? query.OrderByDescending(film => film.Title) : query.OrderBy(film => film.Title)
        };
        return ordered.ThenBy(film => film.Id);
    }
}
