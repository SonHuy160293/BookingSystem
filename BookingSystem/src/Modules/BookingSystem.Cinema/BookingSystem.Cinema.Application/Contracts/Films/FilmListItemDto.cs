namespace BookingSystem.Cinema.Application.Contracts.Films;

public sealed record FilmListItemDto(
    Guid Id, string Title, string? EnglishTitle, short DurationMinutes, DateOnly? ReleaseDate,
    string? AgeClassificationCode, string? PosterUrl, DateTimeOffset CreatedAt);
