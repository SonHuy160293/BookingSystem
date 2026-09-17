using BookingSystem.ApiGateway.Configuration;
using BookingSystem.ApiGateway.Logging;
using BookingSystem.ApiGateway.Middleware;
using BookingSystem.ApiGateway.Routing;
using BookingSystem.ApiGateway.Security;
using BookingSystem.AspNetCore.Middleware;
using BookingSystem.ServiceDefaults;
using Serilog;

var bootstrap = new LoggerConfiguration();
GatewayLogging.Configure(bootstrap);
Log.Logger = bootstrap.CreateLogger();

try
{
    Log.Information(GatewayLogging.StartingTemplate);
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.AddServiceDefaults();
    builder.Host.UseSerilog((_, _, logger) => GatewayLogging.Configure(logger), writeToProviders: true);

    // Freeze policy/route settings together. Discovery still updates destination addresses.
    var proxyConfiguration = new ConfigurationBuilder()
        .AddInMemoryCollection(builder.Configuration.GetSection("ReverseProxy").AsEnumerable())
        .Build().GetSection("ReverseProxy");
    builder.Services.AddGateway(builder.Configuration, proxyConfiguration);
    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseCorrelationId();
    app.UseMiddleware<GatewayBoundaryMiddleware>();
    app.UseMiddleware<GatewaySecurityMiddleware>();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/health"),
            branch => branch.UseHttpsRedirection());
    }

    app.UseRouting();
    app.UseCors();
    app.UseRequestTimeouts();
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapGatewayEndpoints(proxyConfiguration);
    await app.RunAsync();
    Log.Information(GatewayLogging.StoppedTemplate);
    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // Exception messages/configuration can contain credentials or destination URLs.
    Log.Fatal(GatewayLogging.FailedTemplate);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
