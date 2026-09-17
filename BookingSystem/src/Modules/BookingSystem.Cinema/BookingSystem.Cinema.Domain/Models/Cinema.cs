using BookingSystem.Cinema.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Cinema.Domain.Models;

public sealed class Cinema : Entity<Guid>
{
    private Cinema() { }

    private Cinema(
        Guid id,
        Guid tenantId,
        int regionId,
        string name,
        string slug,
        string address,
        PublicationStatus publicationStatus,
        OperatingStatus operatingStatus,
        bool onlineBookingEnabled,
        string? description,
        decimal? latitude,
        decimal? longitude)
        : base(id)
    {
        TenantId = tenantId;
        ChangeDetails(regionId, name, slug, address, description, latitude, longitude);
        ChangeAvailability(publicationStatus, operatingStatus, onlineBookingEnabled);
    }

    public Guid TenantId { get; private set; }
    public int RegionId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public PublicationStatus PublicationStatus { get; private set; }
    public OperatingStatus OperatingStatus { get; private set; }
    public bool OnlineBookingEnabled { get; private set; }
    public Region Region { get; private set; } = null!;

    public static Cinema Create(
        Guid tenantId,
        Guid branchId,
        int regionId,
        string name,
        string slug,
        string address,
        PublicationStatus publicationStatus,
        OperatingStatus operatingStatus,
        bool onlineBookingEnabled,
        string? description = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        ModelValidation.RequiredId(tenantId, nameof(TenantId));
        ModelValidation.RequiredId(branchId, nameof(Id));

        return new Cinema(
            branchId,
            tenantId,
            regionId,
            name,
            slug,
            address,
            publicationStatus,
            operatingStatus,
            onlineBookingEnabled,
            description,
            latitude,
            longitude);
    }

    public void ChangeDetails(
        int regionId,
        string name,
        string slug,
        string address,
        string? description,
        decimal? latitude,
        decimal? longitude)
    {
        ModelValidation.PositiveId(regionId, nameof(RegionId));
        string normalizedName = ModelValidation.RequiredText(name, 250, nameof(Name));
        string normalizedSlug = ModelValidation.Slug(slug, 250);
        string normalizedAddress = ModelValidation.RequiredText(address, 1000, nameof(Address));
        string? normalizedDescription = ModelValidation.OptionalText(description, null, nameof(Description));

        if (latitude.HasValue != longitude.HasValue)
        {
            throw ModelValidation.Invalid(nameof(Latitude), "Latitude and Longitude must be supplied together.");
        }

        if (latitude is < -90 or > 90)
        {
            throw ModelValidation.Invalid(nameof(Latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw ModelValidation.Invalid(nameof(Longitude), "Longitude must be between -180 and 180.");
        }

        RegionId = regionId;
        Name = normalizedName;
        Slug = normalizedSlug;
        Address = normalizedAddress;
        Description = normalizedDescription;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void ChangeAvailability(
        PublicationStatus publicationStatus,
        OperatingStatus operatingStatus,
        bool onlineBookingEnabled)
    {
        if (!Enum.IsDefined(publicationStatus))
        {
            throw ModelValidation.Invalid(nameof(PublicationStatus), "PublicationStatus is invalid.");
        }

        if (!Enum.IsDefined(operatingStatus))
        {
            throw ModelValidation.Invalid(nameof(OperatingStatus), "OperatingStatus is invalid.");
        }

        PublicationStatus = publicationStatus;
        OperatingStatus = operatingStatus;
        OnlineBookingEnabled = onlineBookingEnabled;
    }
}
