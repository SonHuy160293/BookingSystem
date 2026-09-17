using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookingSystem.HttpClients;

/// <summary>Per-client dependencies, also constructible with a fake handler and token provider.</summary>
public sealed class HttpApiClientContext
{
    public HttpApiClientContext(
        string dependencyName,
        HttpApiClientOptions options,
        bool isInternal,
        ICorrelationIdAccessor? correlationIdAccessor = null,
        IServiceTokenProvider? tokenProvider = null,
        ILogger<JsonApiClient>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyName);
        ArgumentNullException.ThrowIfNull(options);
        OptionsValidation.Validate(dependencyName, options, isInternal);
        if (options.UseServiceToken && tokenProvider is null)
        {
            throw new InvalidOperationException("A dependency-keyed service token provider is required.");
        }

        DependencyName = dependencyName;
        Options = options;
        IsInternal = isInternal;
        CorrelationIdAccessor = correlationIdAccessor;
        TokenProvider = tokenProvider;
        Logger = logger ?? NullLogger<JsonApiClient>.Instance;
    }

    public string DependencyName { get; }

    internal HttpApiClientOptions Options { get; }

    internal bool IsInternal { get; }

    internal ICorrelationIdAccessor? CorrelationIdAccessor { get; }

    internal IServiceTokenProvider? TokenProvider { get; }

    internal ILogger<JsonApiClient> Logger { get; }
}
