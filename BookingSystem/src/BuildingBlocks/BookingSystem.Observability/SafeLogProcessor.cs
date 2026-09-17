using OpenTelemetry;
using OpenTelemetry.Logs;

namespace BookingSystem.Observability;

public sealed class SafeLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        var attributes = new List<KeyValuePair<string, object?>>();
        string? template = null;
        foreach (var attribute in data.Attributes ?? [])
        {
            if (attribute.Key == "{OriginalFormat}" && attribute.Value is string value)
            {
                template = value;
            }
            else if (attribute.Key is "ElapsedMilliseconds" or "Elapsed" or "StatusCode"
                && attribute.Value is int or long or double)
            {
                attributes.Add(attribute);
            }
            else if (attribute.Key is "RequestName" or "RequestType" && attribute.Value is string name
                && name.Length <= 200 && name.All(character => char.IsLetterOrDigit(character) || character == '_'))
            {
                attributes.Add(attribute);
            }
            else if (attribute.Key == "CorrelationId" && Guid.TryParse(attribute.Value?.ToString(), out var id))
            {
                attributes.Add(new("CorrelationId", id.ToString("D")));
            }
        }

        // Export reviewed static templates only. Arbitrary formatted messages and exceptions
        // can contain SQL, passwords, reset links, or personal data. Local Serilog sinks retain them.
        data.Body = template switch
        {
            "Handling {RequestType}" or "Handled {RequestType}"
                or "Request {RequestName} completed in {ElapsedMilliseconds} ms"
                or "Long running request: {RequestName} took {ElapsedMilliseconds} ms" => template,
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms" => "HTTP request completed",
            _ => "Application log"
        };
        data.FormattedMessage = null;
        data.Exception = null;
        data.TraceState = null;
        data.Attributes = attributes;
    }
}
