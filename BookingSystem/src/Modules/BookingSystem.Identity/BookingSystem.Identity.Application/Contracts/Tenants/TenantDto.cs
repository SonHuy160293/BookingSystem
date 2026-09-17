namespace BookingSystem.Identity.Application.Contracts.Tenants;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxCode,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
