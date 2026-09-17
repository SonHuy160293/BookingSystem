using System.Globalization;
using BookingSystem.Cinema.Infrastructure.DependencyInjection;
using BookingSystem.Cinema.Infrastructure.Persistence;
using BookingSystem.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

IHost? host = null;

try
{
    Log.Information("Starting BookingSystem.Cinema MigrationRunner");

    var environmentName = GetEnvironmentName();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        EnvironmentName = environmentName,
        ContentRootPath = AppContext.BaseDirectory
    });
    builder.Configuration.AddConfiguration(configuration);
    builder.Logging.ClearProviders();
    builder.AddBookingSystemObservability();
    builder.Services.AddInfrastructure(configuration);
    builder.Services.AddSerilog(
        (services, logger) => logger
        .Enrich.WithProperty("Application", "BookingSystem.Cinema.MigrationRunner")
        .ReadFrom.Configuration(configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId(),
        writeToProviders: true);
    host = builder.Build();
    await host.StartAsync();

    await using var scope = host.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingSystemCinemaDbContext>();

    Log.Information("Applying database migrations");
    await dbContext.Database.MigrateAsync();
    Log.Information("Database is up to date");
}
catch (Exception exception)
{
    Log.Fatal(exception, "BookingSystem.Cinema MigrationRunner terminated unexpectedly");
    throw;
}
finally
{
    if (host is not null)
    {
        await host.StopAsync();
    }

    Log.CloseAndFlush();
    host?.Dispose();
}

static string GetEnvironmentName()
    => Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
       ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
       ?? "Production";
