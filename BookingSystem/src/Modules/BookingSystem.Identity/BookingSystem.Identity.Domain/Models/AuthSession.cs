using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Identity.Domain.Models;

public sealed class AuthSession : IEntity<Guid>
{
    private AuthSession()
    {
    }

    private AuthSession(
        Guid id,
        Guid userId,
        string clientId,
        string? deviceName,
        string? securityStamp,
        DateTime now,
        TimeSpan idleLifetime,
        TimeSpan absoluteLifetime)
    {
        Id = id;
        UserId = userId;
        ClientId = clientId;
        DeviceName = deviceName;
        SecurityStampAtLogin = securityStamp;
        CreatedAt = now;
        LastRefreshedAt = now;
        IdleExpiresAt = now.Add(idleLifetime);
        AbsoluteExpiresAt = now.Add(absoluteLifetime);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? SecurityStampAtLogin { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastRefreshedAt { get; private set; }
    public DateTime IdleExpiresAt { get; private set; }
    public DateTime AbsoluteExpiresAt { get; private set; }
    public Guid CurrentRefreshTokenId { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static AuthSession Create(
        Guid userId,
        string clientId,
        string? deviceName,
        string? securityStamp,
        DateTime now,
        TimeSpan idleLifetime,
        TimeSpan absoluteLifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        if (userId == Guid.Empty || clientId.Length > 128 || deviceName?.Length > 128 || now.Kind != DateTimeKind.Utc || idleLifetime <= TimeSpan.Zero || absoluteLifetime < idleLifetime)
        {
            throw new ArgumentException("Invalid authentication session parameters.");
        }

        return new AuthSession(Guid.CreateVersion7(), userId, clientId, deviceName, securityStamp, now, idleLifetime, absoluteLifetime);
    }

    public bool IsActive(DateTime now) => RevokedAt is null && now < IdleExpiresAt && now < AbsoluteExpiresAt;
    public void SetInitialToken(Guid tokenId)
    {
        if (tokenId == Guid.Empty || CurrentRefreshTokenId != Guid.Empty)
        {
            throw new InvalidOperationException("An initial refresh token must be assigned exactly once.");
        }

        CurrentRefreshTokenId = tokenId;
    }

    public void Rotate(
        Guid expectedTokenId,
        Guid newTokenId,
        DateTime now,
        TimeSpan idleLifetime)
    {
        if (!IsActive(now) || CurrentRefreshTokenId != expectedTokenId || newTokenId == Guid.Empty || newTokenId == expectedTokenId || now.Kind != DateTimeKind.Utc || idleLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("The authentication session cannot rotate.");
        }

        CurrentRefreshTokenId = newTokenId;
        LastRefreshedAt = now;
        IdleExpiresAt = new DateTime(Math.Min(now.Add(idleLifetime).Ticks, AbsoluteExpiresAt.Ticks), DateTimeKind.Utc);
    }

    public void Revoke(DateTime now, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (now.Kind != DateTimeKind.Utc || reason.Length > 64)
        {
            throw new ArgumentException("Invalid revocation parameters.");
        }

        if (RevokedAt is null)
        {
            RevokedAt = now;
            RevokedReason = reason;
        }
    }
}
