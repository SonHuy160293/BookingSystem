using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Identity.Domain.Models;

public sealed class Role : Entity<Guid>
{
    private Role() { }

    private Role(Guid id, string? description, string? name, string? normalizedName, string? concurrencyStamp)
        : base(id)
    {
        Description = description;
        Name = name;
        NormalizedName = normalizedName;
        ConcurrencyStamp = concurrencyStamp;
    }

    public string? Description { get; private set; }
    public string? Name { get; private set; }
    public string? NormalizedName { get; private set; }
    public string? ConcurrencyStamp { get; private set; }

    public static Role Create(string? description, string? name, string? normalizedName, string? concurrencyStamp)
    {
        return new Role(Guid.CreateVersion7(), description, name, normalizedName, concurrencyStamp);
    }
}
