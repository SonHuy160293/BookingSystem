using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Identity.Domain.Models;

public sealed class RefreshToken : IEntity<Guid>
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid sessionId,
        byte[] tokenHash,
        DateTime now,
        DateTime expiresAt)
    {
        Id = id;
        SessionId = sessionId;
        TokenHash = tokenHash.ToArray();
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public byte[] TokenHash { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static RefreshToken Create(
        Guid sessionId,
        byte[] tokenHash,
        DateTime now,
        DateTime expiresAt)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        if (sessionId == Guid.Empty || tokenHash.Length != 32 || now.Kind != DateTimeKind.Utc || expiresAt.Kind != DateTimeKind.Utc || expiresAt <= now)
        {
            throw new ArgumentException("Invalid refresh token parameters.");
        }

        return new RefreshToken(Guid.CreateVersion7(), sessionId, tokenHash, now, expiresAt);
    }

    public void Consume(DateTime now, Guid successorId)
    {
        if (ConsumedAt is not null || now >= ExpiresAt || now.Kind != DateTimeKind.Utc || successorId == Guid.Empty || successorId == Id)
        {
            throw new InvalidOperationException("The refresh token cannot be consumed.");
        }

        ConsumedAt = now;
        ReplacedByTokenId = successorId;
    }
}
