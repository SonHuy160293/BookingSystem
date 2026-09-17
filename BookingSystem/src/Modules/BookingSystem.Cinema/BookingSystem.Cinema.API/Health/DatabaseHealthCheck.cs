using BookingSystem.Cinema.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BookingSystem.Cinema.API.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly BookingSystemCinemaDbContext _dbContext;

    public DatabaseHealthCheck(BookingSystemCinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
    }
}
