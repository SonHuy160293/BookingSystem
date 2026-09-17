namespace BookingSystem.Identity.Application.Contracts.Tenants;

public sealed record CreateTenantRequest(
    string Name,
    string? LegalName = null,
    string? TaxCode = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? Address = null);
