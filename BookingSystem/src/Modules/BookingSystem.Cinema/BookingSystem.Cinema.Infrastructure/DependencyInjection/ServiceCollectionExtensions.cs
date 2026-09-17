using BookingSystem.EntityFrameworkCore.Interceptors;
using BookingSystem.Cinema.Infrastructure.DependencyInjection.Options;
using BookingSystem.Cinema.Infrastructure.Persistence;
using BookingSystem.Cinema.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Cinema.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddPersistence(configuration)
            .AddUnitOfWork();

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetApplicationConnectionString(configuration);
        var postgreSqlOptions = GetPostgreSqlOptions(configuration);

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddSingleton<SqlConnectionFactory>();

        services.AddDbContext<BookingSystemCinemaDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString, postgresOptions =>
                postgresOptions.EnableRetryOnFailure(
                    postgreSqlOptions.MaxRetryCount,
                    TimeSpan.FromSeconds(postgreSqlOptions.MaxRetryDelaySeconds),
                    null))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services;
    }

    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        => services.AddScoped<IUnitOfWork, EfUnitOfWork>();

    private static string GetApplicationConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ApplicationDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ApplicationDb' is missing.");
        }

        return connectionString;
    }

    private static PostgreSqlOptions GetPostgreSqlOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(PostgreSqlOptions.SectionName);

        return new PostgreSqlOptions
        {
            MaxRetryCount = ReadInt(section, nameof(PostgreSqlOptions.MaxRetryCount), 3),
            MaxRetryDelaySeconds = ReadInt(section, nameof(PostgreSqlOptions.MaxRetryDelaySeconds), 10)
        };
    }

    private static int ReadInt(IConfiguration section, string key, int defaultValue)
        => int.TryParse(section[key], out var value) ? value : defaultValue;
}
