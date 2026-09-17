using BookingSystem.EntityFrameworkCore.Interceptors;
using BookingSystem.Cart.Infrastructure.DependencyInjection.Options;
using BookingSystem.Cart.Infrastructure.Persistence;
using BookingSystem.Cart.Infrastructure.Persistence.Core;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Cart.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddPersistence(configuration)
            .AddUnitOfWork();

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetApplicationConnectionString(configuration);
        var sqlServerOptions = GetSqlServerOptions(configuration);

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddSingleton<SqlConnectionFactory>();

        services.AddDbContext<BookingSystemCartDbContext>((serviceProvider, options) =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    sqlServerOptions.MaxRetryCount,
                    TimeSpan.FromSeconds(sqlServerOptions.MaxRetryDelaySeconds),
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

    private static SqlServerOptions GetSqlServerOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(SqlServerOptions.SectionName);

        return new SqlServerOptions
        {
            MaxRetryCount = ReadInt(section, nameof(SqlServerOptions.MaxRetryCount), 3),
            MaxRetryDelaySeconds = ReadInt(section, nameof(SqlServerOptions.MaxRetryDelaySeconds), 10)
        };
    }

    private static int ReadInt(IConfiguration section, string key, int defaultValue)
        => int.TryParse(section[key], out var value) ? value : defaultValue;
}
