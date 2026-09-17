using BookingSystem.Identity.Worker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Identity.Worker.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityWorker(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddHostedService<AuthSessionCleanupService>();
    }
}
