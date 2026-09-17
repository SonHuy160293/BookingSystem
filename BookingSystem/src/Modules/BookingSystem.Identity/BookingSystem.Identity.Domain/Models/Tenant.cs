using BookingSystem.SharedKernel.Abstractions.Domain;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Domain.Models;

public sealed class Tenant : Entity<Guid>
{
    private Tenant() { }

    private Tenant(
        Guid id,
        string name,
        string? legalName,
        string? taxCode,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? slug,
        string? logoUrl)
        : base(id)
    {
        Name = name;
        LegalName = legalName;
        TaxCode = taxCode;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Address = address;
        ChangeBranding(slug, logoUrl);
        IsActive = true;
    }

    public string Name { get; private set; } = null!;
    public string? Slug { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? LegalName { get; private set; }
    public string? TaxCode { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    public static Tenant Create(
        string name,
        string? legalName = null,
        string? taxCode = null,
        string? contactEmail = null,
        string? contactPhone = null,
        string? address = null,
        string? slug = null,
        string? logoUrl = null)
    {
        var details = NormalizeDetails(name, legalName, taxCode, contactEmail, contactPhone, address);

        return new Tenant(
            Guid.CreateVersion7(),
            details.Name,
            details.LegalName,
            details.TaxCode,
            details.ContactEmail,
            details.ContactPhone,
            details.Address,
            slug,
            logoUrl);
    }

    public void ChangeDetails(
        string name,
        string? legalName,
        string? taxCode,
        string? contactEmail,
        string? contactPhone,
        string? address)
    {
        (Name, LegalName, TaxCode, ContactEmail, ContactPhone, Address) =
            NormalizeDetails(name, legalName, taxCode, contactEmail, contactPhone, address);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void ChangeBranding(string? slug, string? logoUrl)
    {
        string? normalizedSlug = string.IsNullOrWhiteSpace(slug)
            ? null
            : NormalizeText(slug, 250, nameof(Slug))!.ToLowerInvariant();

        if (normalizedSlug is not null &&
            (normalizedSlug[0] == '-' || normalizedSlug[^1] == '-' ||
             normalizedSlug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ValidationException(
                [new ValidationError(nameof(Slug), "Slug must contain letters, digits, or hyphens and begin and end with a letter or digit.")]);
        }

        string? normalizedLogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();

        Slug = normalizedSlug;
        LogoUrl = normalizedLogoUrl;
    }

    private static (string Name, string? LegalName, string? TaxCode, string? ContactEmail, string? ContactPhone, string? Address) NormalizeDetails(
        string name,
        string? legalName,
        string? taxCode,
        string? contactEmail,
        string? contactPhone,
        string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException([new ValidationError(nameof(Name), "Name is required.")]);
        }

        string normalizedName = NormalizeText(name, 250, nameof(Name))!;
        string? normalizedLegalName = NormalizeText(legalName, 250, nameof(LegalName));
        string? normalizedTaxCode = NormalizeText(taxCode, 50, nameof(TaxCode));
        string? normalizedContactEmail = NormalizeText(contactEmail, 256, nameof(ContactEmail));
        string? normalizedContactPhone = NormalizeText(contactPhone, 32, nameof(ContactPhone));
        string? normalizedAddress = NormalizeText(address, 500, nameof(Address));

        return (
            normalizedName,
            normalizedLegalName,
            normalizedTaxCode,
            normalizedContactEmail,
            normalizedContactPhone,
            normalizedAddress);
    }

    private static string? NormalizeText(string? value, int maxLength, string propertyName)
    {
        string? normalizedValue = value?.Trim();

        if (normalizedValue is not null && normalizedValue.Length > maxLength)
        {
            throw new ValidationException(
                [new ValidationError(propertyName, $"{propertyName} must not exceed {maxLength} characters.")]);
        }

        return normalizedValue;
    }
}
