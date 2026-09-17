namespace BookingSystem.Cinema.Infrastructure.DependencyInjection.Options;

public sealed class PostgreSqlOptions
{
    public const string SectionName = "PostgreSql";

    public int MaxRetryCount { get; init; } = 3;

    public int MaxRetryDelaySeconds { get; init; } = 10;
}
