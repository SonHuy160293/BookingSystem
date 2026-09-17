using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Abstractions.Services;
using BookingSystem.Identity.Application.Features.Auth.Commands.CleanupSessions;
using BookingSystem.SharedKernel.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingSystem.Identity.Worker.Services;

public sealed class AuthSessionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly AuthSessionOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<AuthSessionCleanupService> _logger;
    public AuthSessionCleanupService(
        IServiceScopeFactory scopes,
        IOptions<AuthSessionOptions> options,
        TimeProvider clock,
        ILogger<AuthSessionCleanupService> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.CleanupEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1), _clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError("Authentication session cleanup failed: {FailureType}", exception.GetType().Name);
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        // Bound each daily pass as well as each transaction; multiple replicas are safe.
        for (int pass = 0; pass < 100; pass++)
        {
            IReadOnlyList<Guid> owners;
            await using (var scope = _scopes.CreateAsyncScope())
            {
                owners = await scope.ServiceProvider.GetRequiredService<IAuthSessionRepository>().FindCleanupUsersAsync(
                    _clock.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays),
                    _options.CleanupBatchSize,
                    cancellationToken);
            }

            if (owners.Count == 0)
            {
                return;
            }

            foreach (var owner in owners)
            {
                await using var scope = _scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(new CleanupSessionsCommand(owner), cancellationToken);
            }
        }
    }
}
