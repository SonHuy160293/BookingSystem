namespace BookingSystem.HttpClients;

public sealed class HttpApiRequest
{
    public required HttpMethod Method { get; init; }

    public required string Path { get; init; }

    public IReadOnlyDictionary<string, string> RouteValues { get; init; } = new Dictionary<string, string>();

    public IReadOnlyCollection<KeyValuePair<string, string?>> QueryParameters { get; init; } = [];

    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    public object? Body { get; init; }

    public Guid? CorrelationId { get; init; }
}
