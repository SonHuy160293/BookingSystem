namespace BookingSystem.Identity.Application.Contracts.Tenants;

public sealed record UpdateTenantRequest(
    string Name,
    bool? IsActive = null,
    string? LegalName = null,
    string? TaxCode = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? Address = null);
