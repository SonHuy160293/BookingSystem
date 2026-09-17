namespace BookingSystem.Identity.Domain.Models;

public sealed class UserClaim
{
    private UserClaim() { }

    private UserClaim(Guid userId, string? claimType, string? claimValue)
    {
        UserId = userId;
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? ClaimType { get; private set; }
    public string? ClaimValue { get; private set; }

    public static UserClaim Create(Guid userId, string? claimType, string? claimValue)
    {
        return new UserClaim(userId, claimType, claimValue);
    }
}
