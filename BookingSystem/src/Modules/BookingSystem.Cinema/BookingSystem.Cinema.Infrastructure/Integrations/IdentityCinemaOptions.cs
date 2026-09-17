namespace BookingSystem.Cinema.Infrastructure.Integrations;

public sealed class IdentityCinemaOptions
{
    public const string SectionName = "IdentityIntegration";
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string HeaderName { get; init; } = "X-Integration-Key";
}
