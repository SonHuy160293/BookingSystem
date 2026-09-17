using BookingSystem.AspNetCore.DependencyInjection;
using BookingSystem.AspNetCore.Filters;
using BookingSystem.AspNetCore.Middleware;
using BookingSystem.AspNetCore.Options;
using BookingSystem.Identity.API.Health;
using BookingSystem.Identity.Application.DependencyInjection;
using BookingSystem.Identity.Infrastructure.DependencyInjection;
using BookingSystem.SharedKernel.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    Log.Information("Starting Company.Project API");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddConfiguredSerilog();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    //builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiResponseEnvelopeFilter>();
    });
    builder.Services.AddConfiguredCors(builder.Configuration);
    //builder.Services.AddConfiguredAuthentication(builder.Configuration, builder.Environment);
    //builder.Services.AddConfiguredAuthorization();
    //builder.Services.Configure<ForwardedHeadersOptions>(options =>
    //{
    //    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    //    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    //    {
    //        if (IPAddress.TryParse(proxy, out var proxyAddress))
    //        {
    //            options.KnownProxies.Add(proxyAddress);
    //        }
    //    }
    //});
    //builder.Services.AddRateLimiter(options =>
    //{
    //    var permitLimit = builder.Configuration.GetValue("RateLimiting:AuthPublic:PermitLimit", 5);
    //    var windowMinutes = builder.Configuration.GetValue("RateLimiting:AuthPublic:WindowMinutes", 10);

    //    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    //    options.AddPolicy(RateLimitPolicyNames.AuthPublic, httpContext =>
    //    {
    //        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
    //            ?? IPAddress.Loopback.ToString();
    //        var endpointKey = httpContext.Request.Path.Value ?? "unknown";
    //        var clientKey = $"{clientIp}:{endpointKey}";

    //        return RateLimitPartition.GetFixedWindowLimiter(
    //            clientKey,
    //            _ => new FixedWindowRateLimiterOptions
    //            {
    //                PermitLimit = permitLimit,
    //                Window = TimeSpan.FromMinutes(windowMinutes),
    //                QueueLimit = 0,
    //                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    //            });
    //    });
    //});
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
        app.UseHttpsRedirection();
    }

    app.UseCors(CorsOptions.PolicyName);
    //app.UseRateLimiter();
    //app.UseAuthentication();
    //app.UseAuthorization();

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
catch (Exception exception)
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
