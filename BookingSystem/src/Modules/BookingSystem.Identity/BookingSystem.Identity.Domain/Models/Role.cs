using BookingSystem.Identity.Domain.Enums;
using BookingSystem.SharedKernel.Abstractions.Domain;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Domain.Models;

public sealed class Role : Entity<Guid>
{
    private Role() { }

    private Role(
        Guid id,
        string? description,
        string? name,
        string? normalizedName,
        string? concurrencyStamp,
        Guid? tenantId,
        RbacScopeType scopeType)
        : base(id)
    {
        Description = description;
        Name = name;
        NormalizedName = normalizedName;
        ConcurrencyStamp = concurrencyStamp;
        TenantId = tenantId;
        ScopeType = scopeType;
    }

    public string? Description { get; private set; }
    public string? Name { get; private set; }
    public string? NormalizedName { get; private set; }
    public string? ConcurrencyStamp { get; private set; }

    // Null tenant ownership makes this definition reusable across the platform; ScopeType controls assignment scope.
    public Guid? TenantId { get; private set; }
    public RbacScopeType ScopeType { get; private set; }

    public static Role Create(
        string? description,
        string? name,
        string? normalizedName,
        string? concurrencyStamp,
        Guid? tenantId = null,
        RbacScopeType scopeType = RbacScopeType.Platform)
    {
        if (!Enum.IsDefined(scopeType))
        {
            throw new ValidationException(
                [new ValidationError(nameof(ScopeType), "ScopeType must be PLATFORM, TENANT, or BRANCH.")]);
        }

        if (tenantId == Guid.Empty)
        {
            throw new ValidationException(
                [new ValidationError(nameof(TenantId), "TenantId must not be empty when provided.")]);
        }

        if (scopeType == RbacScopeType.Platform && tenantId is not null)
        {
            throw new ValidationException(
                [new ValidationError(nameof(TenantId), "PLATFORM roles cannot be owned by a tenant.")]);
        }

        return new Role(Guid.CreateVersion7(), description, name, normalizedName, concurrencyStamp, tenantId, scopeType);
    }
}
