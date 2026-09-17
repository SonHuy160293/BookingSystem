namespace BookingSystem.Cinema.Domain.Models;

public sealed class Region
{
    private Region() { }

    private Region(int id, string name, string slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;

    public static Region Create(int id, string name, string slug)
    {
        ModelValidation.PositiveId(id, nameof(Id));

        return new Region(
            id,
            ModelValidation.RequiredText(name, 250, nameof(Name)),
            ModelValidation.Slug(slug, 250));
    }
}
