using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.Identity.Domain.Models;

namespace BookingSystem.Identity.Application.Mappings;

public static class TenantMapping
{
    public static TenantDto ToDto(Tenant tenant)
        => new(
            tenant.Id,
            tenant.Name,
            tenant.LegalName,
            tenant.TaxCode,
            tenant.ContactEmail,
            tenant.ContactPhone,
            tenant.Address,
            tenant.IsActive,
            tenant.CreatedAt,
            tenant.UpdatedAt);
}
