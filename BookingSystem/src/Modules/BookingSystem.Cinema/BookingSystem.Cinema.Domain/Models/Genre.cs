namespace BookingSystem.Cinema.Domain.Models;

public sealed class Genre
{
    private Genre() { }

    private Genre(int id, string name, string slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;

    public static Genre Create(int id, string name, string slug)
    {
        ModelValidation.PositiveId(id, nameof(Id));

        return new Genre(
            id,
            ModelValidation.RequiredText(name, 100, nameof(Name)),
            ModelValidation.Slug(slug, 100));
    }
}
