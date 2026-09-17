using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class Film : Entity<Guid>
{
    private readonly List<FilmGenre> _genres = [];
    private string[] _countryCodes = [];
    private string[] _formatTags = [];
    private string[] _captionOptions = [];

    private Film() { }

    private Film(
        Guid id,
        string title,
        short durationMinutes,
        DateOnly? releaseDate,
        string? englishTitle,
        string? synopsis,
        string? englishSynopsis,
        string? ageClassificationCode)
        : base(id)
    {
        ChangeDetails(title, durationMinutes, releaseDate, englishTitle, synopsis, englishSynopsis, ageClassificationCode);
    }

    public string Title { get; private set; } = null!;
    public string? EnglishTitle { get; private set; }
    public string? Synopsis { get; private set; }
    public string? EnglishSynopsis { get; private set; }
    public short DurationMinutes { get; private set; }
    public DateOnly? ReleaseDate { get; private set; }
    public string? AgeClassificationCode { get; private set; }
    public string? PosterUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? TrailerUrl { get; private set; }
    public string? TrailerVideoUrl { get; private set; }
    public IReadOnlyList<string> CountryCodes => Array.AsReadOnly(_countryCodes);
    public IReadOnlyList<string> FormatTags => Array.AsReadOnly(_formatTags);
    public IReadOnlyList<string> CaptionOptions => Array.AsReadOnly(_captionOptions);
    public IReadOnlyCollection<FilmGenre> Genres => _genres.AsReadOnly();

    public static Film Create(
        string title,
        short durationMinutes,
        DateOnly? releaseDate = null,
        string? englishTitle = null,
        string? synopsis = null,
        string? englishSynopsis = null,
        string? ageClassificationCode = null)
        => new(
            Guid.CreateVersion7(),
            title,
            durationMinutes,
            releaseDate,
            englishTitle,
            synopsis,
            englishSynopsis,
            ageClassificationCode);

    public void ChangeDetails(
        string title,
        short durationMinutes,
        DateOnly? releaseDate,
        string? englishTitle,
        string? synopsis,
        string? englishSynopsis,
        string? ageClassificationCode)
    {
        if (durationMinutes <= 0)
        {
            throw ModelValidation.Invalid(nameof(DurationMinutes), "DurationMinutes must be positive.");
        }

        string normalizedTitle = ModelValidation.RequiredText(title, 500, nameof(Title));
        string? normalizedEnglishTitle = ModelValidation.OptionalText(englishTitle, 500, nameof(EnglishTitle));
        string? normalizedSynopsis = ModelValidation.OptionalText(synopsis, null, nameof(Synopsis));
        string? normalizedEnglishSynopsis = ModelValidation.OptionalText(englishSynopsis, null, nameof(EnglishSynopsis));
        string? normalizedAgeClassificationCode = ModelValidation.OptionalText(ageClassificationCode, 16, nameof(AgeClassificationCode));

        if (string.Equals(normalizedEnglishSynopsis, normalizedSynopsis, StringComparison.Ordinal))
        {
            normalizedEnglishSynopsis = null;
        }

        Title = normalizedTitle;
        EnglishTitle = normalizedEnglishTitle;
        Synopsis = normalizedSynopsis;
        EnglishSynopsis = normalizedEnglishSynopsis;
        DurationMinutes = durationMinutes;
        ReleaseDate = releaseDate;
        AgeClassificationCode = normalizedAgeClassificationCode;
    }

    public void ChangeMedia(string? posterUrl, string? bannerUrl, string? trailerUrl, string? trailerVideoUrl)
    {
        PosterUrl = ModelValidation.OptionalText(posterUrl, null, nameof(PosterUrl));
        BannerUrl = ModelValidation.OptionalText(bannerUrl, null, nameof(BannerUrl));
        TrailerUrl = ModelValidation.OptionalText(trailerUrl, null, nameof(TrailerUrl));
        TrailerVideoUrl = ModelValidation.OptionalText(trailerVideoUrl, null, nameof(TrailerVideoUrl));
    }

    public void SetCountryCodes(IEnumerable<string> countryCodes)
        => _countryCodes = ModelValidation.CountryCodes(countryCodes);

    public void SetFormatTags(IEnumerable<string> formatTags)
        => _formatTags = ModelValidation.Tokens(formatTags, nameof(FormatTags));

    public void SetCaptionOptions(IEnumerable<string> captionOptions)
        => _captionOptions = ModelValidation.Tokens(captionOptions, nameof(CaptionOptions));

    public void SetGenres(IEnumerable<Genre> genres)
    {
        ArgumentNullException.ThrowIfNull(genres);
        var canonicalGenres = new SortedDictionary<int, Genre>();

        foreach (Genre genre in genres)
        {
            ArgumentNullException.ThrowIfNull(genre);
            ModelValidation.PositiveId(genre.Id, nameof(Genre.Id));
            canonicalGenres.TryAdd(genre.Id, genre);
        }

        Dictionary<int, FilmGenre> existing = _genres.ToDictionary(membership => membership.GenreId);
        FilmGenre[] memberships = canonicalGenres.Values
            .Select(genre => existing.TryGetValue(genre.Id, out FilmGenre? membership)
                ? membership
                : FilmGenre.Create(this, genre))
            .ToArray();

        _genres.Clear();
        _genres.AddRange(memberships);
    }
}
