using BookingSystem.SharedKernel.Abstractions.Domain;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Domain.Models;

public sealed class Branch : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }

    private Branch() { }

    private Branch(Guid id, Guid tenantId, string name, string? address)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Address = address;
    }

    public static Branch Create(Guid tenantId, string name, string? address = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ValidationException([new ValidationError(nameof(TenantId), "TenantId is required.")]);
        }

        var (trimmedName, trimmedAddress) = NormalizeDetails(name, address);
        return new Branch(Guid.CreateVersion7(), tenantId, trimmedName, trimmedAddress);
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
