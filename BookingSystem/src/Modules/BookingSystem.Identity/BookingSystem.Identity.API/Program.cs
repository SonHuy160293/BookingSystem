using BookingSystem.AspNetCore.DependencyInjection;
using BookingSystem.AspNetCore.Filters;
using BookingSystem.AspNetCore.Middleware;
using BookingSystem.AspNetCore.Options;
using BookingSystem.Identity.API.Health;
using BookingSystem.Identity.Application.DependencyInjection;
using BookingSystem.Identity.Infrastructure.DependencyInjection;
using BookingSystem.Identity.Worker.DependencyInjection;
using BookingSystem.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    Log.Information("Starting Company.Project API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.AddServiceDefaults();

    builder.AddConfiguredSerilog();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddIdentityWorker();

    builder.Services.AddCurrentUserProvider();
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<BookingSystem.Identity.API.Filters.AuthApiResponseEnvelopeFilter>();
    });
    builder.Services.AddConfiguredCors(builder.Configuration);
    builder.Services.AddConfiguredAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddConfiguredAuthorization();
    builder.Services.AddTrustedForwarding(builder.Configuration);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        foreach (string name in new[] { "AuthLogin", "AuthRefresh", "AuthManagement" })
        {
            bool login = name == "AuthLogin";
            string section = login ? "RateLimiting:AuthPublic" : "RateLimiting:AuthRefresh";
            int permits = builder.Configuration.GetValue(section + ":PermitLimit", login ? 5 : 60);
            int minutes = builder.Configuration.GetValue(section + ":WindowMinutes", login ? 10 : 1);
            if (permits <= 0 || minutes <= 0)
            {
                throw new InvalidOperationException("Authentication rate limits must be positive.");
            }

            options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
                name + ":" + (context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permits, Window = TimeSpan.FromMinutes(minutes), QueueLimit = 0
                }));
        }
    });
    builder.Services.AddConfiguredApiVersioning();
    builder.Services
        .AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
        .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

    builder.Services.AddConfiguredSwagger();

    var app = builder.Build();

    Log.Information(
        "Company.Project API configured for {Environment} and listening on {Urls}",
        app.Environment.EnvironmentName,
        app.Configuration["ASPNETCORE_URLS"] ?? "(default)");

    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate");
    var seedDatabase = app.Configuration.GetValue<bool>("Database:Seed");

    Log.Information(
        "Database startup options: AutoMigrate={AutoMigrate}, Seed={Seed}",
        autoMigrate,
        seedDatabase);

    app.UseForwardedHeaders();
    app.UseCorrelationId();
    app.UseGlobalExceptionHandling();
    app.UseMiddleware<BookingSystem.Identity.API.Middleware.AuthDatabaseFailureMiddleware>();
    app.UseApiStatusCodeEnvelope();
    app.UseSerilogRequestLogging();
    app.UseStaticFiles();

    //if (autoMigrate)
    //{
    //    Log.Information("Applying database migrations from API startup");

    //    await using var scope = app.Services.CreateAsyncScope();
    //    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    //    await dbContext.Database.MigrateAsync();
    //    await PermissionDefinitionSeeder.SyncAsync(dbContext);
    //    await MenuDefinitionSeeder.SyncAsync(dbContext);

    //    if (seedDatabase)
    //    {
    //        Log.Information("Seeding database from API startup");
    //        await DatabaseSeeder.SeedAsync(dbContext);

    //        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    //        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    //        await AccountSeeder.SeedRootAccountAsync(userManager, roleManager, app.Configuration);
    //    }
    //}

    app.UseConfiguredSwagger();

    var useFrontendResetPage = app.Configuration.GetValue<bool>("PasswordReset:UseFrontendResetPage");
    var enableBuiltInResetPage = app.Configuration.GetValue<bool>("PasswordReset:EnableBuiltInResetPage");

    if (enableBuiltInResetPage && !useFrontendResetPage)
    {
        app.MapGet("/reset-password", async context =>
        {
            var resetPagePath = Path.Combine(
                app.Environment.ContentRootPath,
                "wwwroot",
                "reset-password.html");

            if (!File.Exists(resetPagePath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("The built-in reset password page was not found.");
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(resetPagePath);
        });
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }
    else
    {
        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/health"),
            branch => branch.UseHttpsRedirection());
    }

    app.UseCors(CorsOptions.PolicyName);
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live"),
        ResponseWriter = WriteHealthCheckResponseAsync
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponseAsync
    });

    app.MapControllers();

    await app.RunAsync();

    Log.Information("Company.Project API stopped cleanly");
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Company.Project API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            duration = entry.Value.Duration.TotalMilliseconds
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(response));
}

public partial class Program { }
