using System.Text.Json.Serialization;
namespace BookingSystem.Cinema.Application.Contracts.Films;

public sealed record UpdateFilmRequest
{
    [JsonRequired]
    public string Title { get; init; } = null!;
    public string? EnglishTitle { get; init; }
    public string? Synopsis { get; init; }
    public string? EnglishSynopsis { get; init; }
    [JsonRequired]
    public short DurationMinutes { get; init; }
    public DateOnly? ReleaseDate { get; init; }
    public string? AgeClassificationCode { get; init; }
    public string? PosterUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? TrailerUrl { get; init; }
    public string? TrailerVideoUrl { get; init; }
    [JsonRequired]
    public string[] CountryCodes { get; init; } = null!;
    [JsonRequired]
    public string[] FormatTags { get; init; } = null!;
    [JsonRequired]
    public string[] CaptionOptions { get; init; } = null!;
    [JsonRequired]
    public int[] GenreIds { get; init; } = null!;
}
