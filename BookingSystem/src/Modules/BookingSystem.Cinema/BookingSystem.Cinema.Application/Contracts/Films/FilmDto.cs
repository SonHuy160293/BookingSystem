namespace BookingSystem.Cinema.Application.Contracts.Films;

public sealed record FilmDto(
    Guid Id, string Title, string? EnglishTitle, string? Synopsis, string? EnglishSynopsis,
    short DurationMinutes, DateOnly? ReleaseDate, string? AgeClassificationCode,
    string? PosterUrl, string? BannerUrl, string? TrailerUrl, string? TrailerVideoUrl,
    IReadOnlyList<string> CountryCodes, IReadOnlyList<string> FormatTags, IReadOnlyList<string> CaptionOptions,
    IReadOnlyList<GenreDto> Genres, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
