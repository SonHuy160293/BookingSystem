namespace BookingSystem.Identity.Domain.Models;

public sealed class RoleClaim
{
    private RoleClaim() { }

    private RoleClaim(Guid roleId, string? claimType, string? claimValue)
    {
        RoleId = roleId;
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    public int Id { get; private set; }
    public Guid RoleId { get; private set; }
    public string? ClaimType { get; private set; }
    public string? ClaimValue { get; private set; }

    public static RoleClaim Create(Guid roleId, string? claimType, string? claimValue)
    {
        return new RoleClaim(roleId, claimType, claimValue);
    }
}
