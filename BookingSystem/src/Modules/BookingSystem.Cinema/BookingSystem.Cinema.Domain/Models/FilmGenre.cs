namespace BookingSystem.Cinema.Domain.Models;

public sealed class FilmGenre
{
    private FilmGenre() { }

    private FilmGenre(Guid filmId, int genreId)
    {
        FilmId = filmId;
        GenreId = genreId;
    }

    public Guid FilmId { get; private set; }
    public int GenreId { get; private set; }
    public Film Film { get; private set; } = null!;
    public Genre Genre { get; private set; } = null!;

    public static FilmGenre Create(Guid filmId, int genreId)
    {
        ModelValidation.RequiredId(filmId, nameof(FilmId));
        ModelValidation.PositiveId(genreId, nameof(GenreId));

        return new FilmGenre(filmId, genreId);
    }

    internal static FilmGenre Create(Film film, Genre genre)
    {
        FilmGenre membership = Create(film.Id, genre.Id);
        membership.Film = film;
        membership.Genre = genre;

        return membership;
    }
}
