using BookingSystem.AspNetCore.HttpClients;
using BookingSystem.HttpClients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingSystem.AspNetCore.DependencyInjection;

public static class HttpClientCorrelationExtensions
{
    public static IServiceCollection AddHttpClientCorrelation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Singleton<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>());
        return services;
    }
}
