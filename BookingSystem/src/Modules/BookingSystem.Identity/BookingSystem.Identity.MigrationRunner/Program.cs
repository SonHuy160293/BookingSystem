using System.Globalization;
using BookingSystem.Identity.Infrastructure.DependencyInjection;
using BookingSystem.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

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

    Log.Logger = new LoggerConfiguration()
        .Enrich.WithProperty("Application", "BookingSystem.Identity.MigrationRunner")
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .CreateLogger();

    Log.Information(
        "BookingSystem.Identity MigrationRunner configured for {Environment}",
        environmentName);

    var seedDatabase = configuration.GetValue<bool>("Database:Seed");
    Log.Information("Database migration options: Seed={Seed}", seedDatabase);

    await using var services = new ServiceCollection()
        .AddInfrastructure(configuration)
        .BuildServiceProvider();

    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingSystemIdentityDbContext>();

    Log.Information("Applying database migrations");
    await dbContext.Database.MigrateAsync();
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
    Log.CloseAndFlush();
}

static string GetEnvironmentName()
    => Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
       ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
       ?? "Production";
