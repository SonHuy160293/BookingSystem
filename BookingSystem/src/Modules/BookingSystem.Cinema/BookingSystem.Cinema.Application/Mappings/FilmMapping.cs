using BookingSystem.Cinema.Application.Contracts.Films;
using BookingSystem.Cinema.Domain.Models;

namespace BookingSystem.Cinema.Application.Mappings;

public static class FilmMapping
{
    public static FilmDto ToDto(Film film)
        => new(film.Id, film.Title, film.EnglishTitle, film.Synopsis, film.EnglishSynopsis,
            film.DurationMinutes, film.ReleaseDate, film.AgeClassificationCode, film.PosterUrl, film.BannerUrl,
            film.TrailerUrl, film.TrailerVideoUrl, film.CountryCodes, film.FormatTags, film.CaptionOptions,
            film.Genres.OrderBy(membership => membership.GenreId)
                .Select(membership => new GenreDto(membership.GenreId, membership.Genre.Name, membership.Genre.Slug)).ToArray(),
            film.CreatedAt, film.UpdatedAt);
}
