using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Identity.Domain.Models;

public sealed class Branch : Entity<Guid>
{
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }

    private Branch() { }

    private Branch(Guid id, string name, string? address)
        : base(id)
    {
        Name = name;
        Address = address;
    }

    public static Branch Create(string name, string? address = null)
    {
        var (trimmedName, trimmedAddress) = NormalizeDetails(name, address);
        return new Branch(Guid.CreateVersion7(), trimmedName, trimmedAddress);
    }

    public void ChangeDetails(string name, string? address)
    {
        (Name, Address) = NormalizeDetails(name, address);
    }

    private static (string Name, string? Address) NormalizeDetails(string name, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string trimmedName = name.Trim();
        string? trimmedAddress = address?.Trim();

        if (trimmedName.Length > 250)
        {
            throw new ArgumentException("Name must not exceed 250 characters.", nameof(name));
        }

        if (trimmedAddress is { Length: > 250 })
        {
            throw new ArgumentException("Address must not exceed 250 characters.", nameof(address));
        }

        return (trimmedName, trimmedAddress);
    }
}
