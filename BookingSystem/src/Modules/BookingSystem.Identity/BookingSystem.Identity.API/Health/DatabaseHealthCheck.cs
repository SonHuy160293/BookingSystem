using BookingSystem.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BookingSystem.Identity.API.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly BookingSystemIdentityDbContext _dbContext;

    public DatabaseHealthCheck(BookingSystemIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("SQL Server is reachable.")
            : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
    }
}
