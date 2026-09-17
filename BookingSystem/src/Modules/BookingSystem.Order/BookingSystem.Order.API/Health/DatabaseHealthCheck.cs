using BookingSystem.Order.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BookingSystem.Order.API.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly BookingSystemOrderDbContext _dbContext;

    public DatabaseHealthCheck(BookingSystemOrderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("SQL Server is reachable.")
            : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
    }
}
