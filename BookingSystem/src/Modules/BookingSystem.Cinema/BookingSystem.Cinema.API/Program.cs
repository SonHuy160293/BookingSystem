using System.Globalization;
using System.Text.Json;
using BookingSystem.AspNetCore.DependencyInjection;
using BookingSystem.AspNetCore.Filters;
using BookingSystem.AspNetCore.Middleware;
using BookingSystem.AspNetCore.Options;
using BookingSystem.Cinema.API.Health;
using BookingSystem.Cinema.Application.DependencyInjection;
using BookingSystem.Cinema.Infrastructure.DependencyInjection;
using BookingSystem.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    Log.Information("Starting BookingSystem.Cinema API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.AddServiceDefaults();

    builder.AddConfiguredSerilog();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiResponseEnvelopeFilter>();
    });
    builder.Services.AddConfiguredCors(builder.Configuration);
    builder.Services.AddConfiguredApiVersioning();
    builder.Services
        .AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
        .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
    builder.Services.AddConfiguredSwagger();

    var app = builder.Build();

    app.UseCorrelationId();
    app.UseGlobalExceptionHandling();
    app.UseApiStatusCodeEnvelope();
    app.UseSerilogRequestLogging();
    app.UseConfiguredSwagger();

    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseCors(CorsOptions.PolicyName);

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
    app.MapDefaultEndpoints();

    await app.RunAsync();

    Log.Information("BookingSystem.Cinema API stopped cleanly");
}
catch (Exception exception)
{
    Log.Fatal(exception, "BookingSystem.Cinema API terminated unexpectedly");
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
