using System.Text.Json;

namespace BookingSystem.HttpClients;

public sealed class HttpApiClientOptions
{
    public string BaseAddress { get; set; } = string.Empty;

    public string? IntegrationKey { get; set; }

    public string? ApiKeyHeaderName { get; set; }

    public string? ApiKey { get; set; }

    public bool UseServiceToken { get; set; }

    public bool ForwardCorrelationId { get; set; }

    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan HandlerLifetime { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public int MaxConnectionsPerServer { get; set; } = 64;

    public int MaxRetryAttempts { get; set; } = 2;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    public bool UseRetryJitter { get; set; } = true;

    public double CircuitFailureRatio { get; set; } = 0.5;

    public int CircuitMinimumThroughput { get; set; } = 20;

    public TimeSpan CircuitSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan CircuitBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    public JsonSerializerOptions JsonOptions { get; set; } = new(JsonSerializerDefaults.Web);
}
