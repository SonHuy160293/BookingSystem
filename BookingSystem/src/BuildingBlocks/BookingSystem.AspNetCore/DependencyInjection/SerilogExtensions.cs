using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace BookingSystem.AspNetCore.DependencyInjection;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddConfiguredSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();
        });

        return builder;
    }
}
