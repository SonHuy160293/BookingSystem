using BookingSystem.EntityFrameworkCore.Interceptors;
using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Infrastructure.DependencyInjection.Options;
using BookingSystem.Identity.Infrastructure.Persistence;
using BookingSystem.Identity.Infrastructure.Persistence.Core;
using BookingSystem.Identity.Infrastructure.Persistence.Repositories;
using BookingSystem.SharedKernel.Abstractions.Persistence;
using BookingSystem.SharedKernel.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Identity.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddPersistence(configuration)
            .AddRepositories()
            .AddUnitOfWork();

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetApplicationConnectionString(configuration);
        var sqlServerOptions = GetSqlServerOptions(configuration);

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddSingleton<SqlConnectionFactory>();

        services.AddDbContext<BookingSystemIdentityDbContext>((serviceProvider, options) =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    sqlServerOptions.MaxRetryCount,
                    TimeSpan.FromSeconds(sqlServerOptions.MaxRetryDelaySeconds),
                    null))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
        => services
            .AddScoped<IUserRepository, UserRepository>();

    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        => services.AddScoped<IUnitOfWork, EfUnitOfWork>();

    //public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    //{
    //    services
    //        .AddOptions<EmailOptions>()
    //        .Bind(configuration.GetSection(EmailOptions.SectionName))
    //        .Validate(IsSupportedEmailProvider, "Email:Provider must be Logging or Smtp.")
    //        .Validate(HasValidFromAddress, "Email:FromAddress is required.")
    //        .Validate(HasValidSmtpConfiguration, "SMTP email provider requires Email:Smtp:Host and a valid Email:Smtp:Port.")
    //        .ValidateOnStart();

    //    services.AddScoped<LoggingEmailSender>();
    //    services.AddScoped<SmtpEmailSender>();
    //    services.AddScoped<IEmailTemplateRenderer, DefaultEmailTemplateRenderer>();

    //    services.AddScoped<IEmailSender>(serviceProvider =>
    //    {
    //        var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;

    //        return options.Provider.Equals(EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase)
    //            ? serviceProvider.GetRequiredService<SmtpEmailSender>()
    //            : serviceProvider.GetRequiredService<LoggingEmailSender>();
    //    });

    //    return services;
    //}

    //public static IServiceCollection AddExternalIntegrations(this IServiceCollection services)
    //{
    //    services.AddHttpClient(ExternalIntegrationDemoService.HttpClientName, client =>
    //    {
    //        client.BaseAddress = new Uri("https://httpbin.org/");
    //        client.Timeout = TimeSpan.FromSeconds(10);
    //        client.DefaultRequestHeaders.Add("User-Agent", "Company.Project.Api");
    //    });

    //    services.AddScoped<IExternalIntegrationDemoService, ExternalIntegrationDemoService>();
    //    services.AddScoped<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();

    //    return services;
    //}

    public static IServiceCollection AddInfrastructureCqrs(this IServiceCollection services)
        => services
            .AddSelfMadeCqrs(typeof(ServiceCollectionExtensions).Assembly);

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

    private static bool IsSupportedEmailProvider(EmailOptions options)
        => options.Provider.Equals(EmailProviders.Logging, StringComparison.OrdinalIgnoreCase)
            || options.Provider.Equals(EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase);

    private static bool HasValidFromAddress(EmailOptions options)
        => !string.IsNullOrWhiteSpace(options.FromAddress);

    private static bool HasValidSmtpConfiguration(EmailOptions options)
        => !options.Provider.Equals(EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(options.Smtp.Host) && options.Smtp.Port > 0);
}
