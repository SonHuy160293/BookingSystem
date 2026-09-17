namespace BookingSystem.Identity.Infrastructure.DependencyInjection.Options;

public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    public int MaxRetryCount { get; init; } = 3;
    public int MaxRetryDelaySeconds { get; init; } = 10;
}
