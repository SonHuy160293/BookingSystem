using System.Globalization;
using BookingSystem.Identity.Infrastructure.DependencyInjection;
using BookingSystem.Identity.Infrastructure.Persistence;
using BookingSystem.Observability;
using Company.Project.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
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
    Log.Information("Starting BookingSystem.Identity MigrationRunner");

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
        .Enrich.WithProperty("Application", "BookingSystem.Identity.MigrationRunner")
        .ReadFrom.Configuration(configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId(),
        writeToProviders: true);
    host = builder.Build();
    await host.StartAsync();

    Log.Information(
        "BookingSystem.Identity MigrationRunner configured for {Environment}",
        environmentName);

    var seedDatabase = configuration.GetValue<bool>("Database:Seed");
    Log.Information("Database migration options: Seed={Seed}", seedDatabase);

    await using var scope = host.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingSystemIdentityDbContext>();

    Log.Information("Applying database migrations");
    await dbContext.Database.MigrateAsync();
    await PermissionDefinitionSeeder.SyncAsync(dbContext);
    Log.Information("Database is up to date");

    if (seedDatabase)
    {
        //Log.Information("Seeding database from MigrationRunner");
        //await DatabaseSeeder.SeedAsync(dbContext);

        //var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        //var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        //await AccountSeeder.SeedRootAccountAsync(userManager, roleManager, configuration);

        //Log.Information("Database seed is up to date");
    }
}
catch (Exception exception)
{
    Log.Fatal(exception, "BookingSystem.Identity MigrationRunner terminated unexpectedly");
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
