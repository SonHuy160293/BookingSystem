namespace BookingSystem.Identity.Application.Abstractions.Services;

public sealed class AuthSessionOptions
{
    public const string SectionName = "AuthSessions";
    public string DefaultClientId { get; set; } = "booking-system";
    public string[] AllowedClientIds { get; set; } = ["booking-system"];
    public int IdleExpirationDays { get; set; } = 7;
    public int AbsoluteExpirationDays { get; set; } = 30;
    public int RetentionDays { get; set; } = 7;
    public int CleanupBatchSize { get; set; } = 100;
    public bool CleanupEnabled { get; set; } = true;

    public bool IsAllowedClient(string clientId) => AllowedClientIds.Contains(clientId, StringComparer.Ordinal);
}
