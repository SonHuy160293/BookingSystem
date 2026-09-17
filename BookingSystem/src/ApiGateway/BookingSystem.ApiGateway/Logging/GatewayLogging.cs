using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BookingSystem.ApiGateway.Logging;

public static class GatewayLogging
{
    public const string RequestTemplate = "Gateway {RequestMethod} {RouteId} via {ClusterId} responded {StatusCode} in {ElapsedMilliseconds} ms; {GatewayErrorCode} {ForwarderErrorCode} {TraceId} {SpanId}";
    public const string StartingTemplate = "Gateway starting";
    public const string StoppedTemplate = "Gateway stopped";
    public const string FailedTemplate = "Gateway startup failed";

    public static void Configure(LoggerConfiguration configuration)
        => configuration.MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.With(new GatewayLogRedactionEnricher())
            .Filter.With(new GatewaySafeLogFilter())
            .WriteTo.Console(
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}");
}

public sealed class GatewaySafeLogFilter : ILogEventFilter
{
    internal static readonly HashSet<string> AllowedProperties =
    [
        "SourceContext", "RequestMethod", "RouteId", "ClusterId", "StatusCode",
        "ElapsedMilliseconds", "CorrelationId", "TraceId", "SpanId",
        "GatewayErrorCode", "ForwarderErrorCode"
    ];

    public bool IsEnabled(LogEvent logEvent)
    {
        if (logEvent.Exception is not null
            || logEvent.MessageTemplate.Text is not (GatewayLogging.RequestTemplate
                or GatewayLogging.StartingTemplate or GatewayLogging.StoppedTemplate or GatewayLogging.FailedTemplate))
        {
            return false;
        }

        foreach (var (key, value) in logEvent.Properties)
        {
            if (!AllowedProperties.Contains(key) || value is not ScalarValue scalar)
            {
                return false;
            }

            bool safe = key switch
            {
                "SourceContext" => scalar.Value is string source && source.StartsWith("BookingSystem.ApiGateway.", StringComparison.Ordinal),
                "StatusCode" => scalar.Value is int status && status is >= 100 and <= 599,
                "ElapsedMilliseconds" => scalar.Value is double elapsed && double.IsFinite(elapsed) && elapsed >= 0,
                "CorrelationId" => Guid.TryParse(scalar.Value?.ToString(), out _),
                "TraceId" => IsHex(scalar.Value, 32),
                "SpanId" => IsHex(scalar.Value, 16),
                "RequestMethod" => scalar.Value is string method
                    && method is "GET" or "HEAD" or "POST" or "PUT" or "PATCH" or "DELETE" or "OPTIONS" or "OTHER",
                _ => scalar.Value is string token && IsSafeToken(token)
            };
            if (!safe)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsHex(object? value, int length)
        => value is string text && (text == "None" || text.Length == length && text.All(char.IsAsciiHexDigit));

    internal static bool IsSafeToken(string text)
        => text.Length is > 0 and <= 100
            && text.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
}

public sealed class GatewayLogRedactionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Hosting scopes can contain raw paths. Remove unreviewed properties before
        // either the local console sink or the OpenTelemetry provider receives them.
        foreach (string name in logEvent.Properties.Keys.ToArray())
        {
            if (!GatewaySafeLogFilter.AllowedProperties.Contains(name))
            {
                logEvent.RemovePropertyIfPresent(name);
            }
        }
    }
}
