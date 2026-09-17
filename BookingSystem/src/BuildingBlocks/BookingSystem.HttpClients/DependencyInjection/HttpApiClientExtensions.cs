using BookingSystem.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Telemetry;
using Polly.Timeout;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpApiClientExtensions
{
    public static IHttpClientBuilder AddInternalApiClient<TInterface, TImplementation>(
        this IServiceCollection services,
        string dependencyName,
        IConfigurationSection configuration)
        where TInterface : class
        where TImplementation : JsonApiClient, TInterface
        => AddClient<TInterface, TImplementation>(services, dependencyName, configuration, isInternal: true);

    public static IHttpClientBuilder AddExternalApiClient<TInterface, TImplementation>(
        this IServiceCollection services,
        string dependencyName,
        IConfigurationSection configuration)
        where TInterface : class
        where TImplementation : JsonApiClient, TInterface
        => AddClient<TInterface, TImplementation>(services, dependencyName, configuration, isInternal: false);

    private static IHttpClientBuilder AddClient<TInterface, TImplementation>(
        IServiceCollection services, string name, IConfigurationSection configuration, bool isInternal)
        where TInterface : class
        where TImplementation : JsonApiClient, TInterface
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!OptionsValidation.IsDependencyName(name))
        {
            throw new ArgumentException("Dependency name must be a bounded ASCII identifier.", nameof(name));
        }

        if (services.Any(descriptor => descriptor.ServiceType == typeof(ClientRegistration)
            && descriptor.ImplementationInstance is ClientRegistration registration
            && string.Equals(registration.Name, name, StringComparison.OrdinalIgnoreCase))
            || services.Any(descriptor => descriptor.ServiceType == typeof(TInterface)))
        {
            throw new InvalidOperationException("Each dependency name and typed-client interface must be registered once.");
        }

        services.AddSingleton(new ClientRegistration(name));
        services.AddLogging();
        services.TryAddSingleton<ICorrelationIdAccessor>(new EmptyCorrelationIdAccessor());
        services.AddSingleton<IValidateOptions<HttpApiClientOptions>>(provider =>
            new ClientOptionsValidator(name, isInternal, provider.GetRequiredService<IServiceProviderIsKeyedService>()));
        services.AddOptions<HttpApiClientOptions>(name)
            .Configure(options => configuration.Bind(options))
            .ValidateOnStart();

        string clientName = "BookingSystem.HttpClients." + name;
        services.AddOptions<HttpClientFactoryOptions>(clientName)
            .Configure<IOptionsMonitor<HttpApiClientOptions>>((factory, options) =>
                factory.HandlerLifetime = options.Get(name).HandlerLifetime);

        var builder = services.AddHttpClient(clientName, (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<HttpApiClientOptions>>().Get(name);
            client.BaseAddress = new Uri(options.BaseAddress.TrimEnd('/') + "/");
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        });
        builder.RemoveAllLoggers();
        builder.RemoveAllResilienceHandlers();
        builder.ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<HttpApiClientOptions>>().Get(name);
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                ConnectTimeout = options.ConnectTimeout,
                PooledConnectionLifetime = options.PooledConnectionLifetime,
                PooledConnectionIdleTimeout = options.PooledConnectionIdleTimeout,
                MaxConnectionsPerServer = options.MaxConnectionsPerServer
            };
        });

        builder.AddResilienceHandler("dependency", (pipeline, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptionsMonitor<HttpApiClientOptions>>().Get(name);
            // Replace the default telemetry listener rather than logging exception details or HTTP metadata.
            pipeline.ConfigureTelemetry(new TelemetryOptions { LoggerFactory = NullLoggerFactory.Instance });
            if (options.MaxRetryAttempts > 0)
            {
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    Delay = options.RetryDelay,
                    MaxDelay = options.MaxRetryDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = options.UseRetryJitter,
                    ShouldRetryAfterHeader = true,
                    ShouldHandle = args =>
                    {
                        HttpMethod? method = args.Context.GetRequestMessage()?.Method;
                        return ValueTask.FromResult(!args.Context.CancellationToken.IsCancellationRequested
                            && (method == HttpMethod.Get || method == HttpMethod.Head) && IsTransient(args.Outcome));
                    },
                    OnRetry = args =>
                    {
                        args.Outcome.Result?.Dispose();
                        return ValueTask.CompletedTask;
                    }
                });
            }

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitFailureRatio,
                MinimumThroughput = options.CircuitMinimumThroughput,
                SamplingDuration = options.CircuitSamplingDuration,
                BreakDuration = options.CircuitBreakDuration,
                ShouldHandle = args => ValueTask.FromResult(
                    !args.Context.CancellationToken.IsCancellationRequested && IsTransient(args.Outcome))
            });
            pipeline.AddTimeout(options.AttemptTimeout);
        });

        builder.AddTypedClient<TInterface>((client, provider) =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<HttpApiClientOptions>>().Get(name);
            var context = new HttpApiClientContext(name, options, isInternal,
                provider.GetRequiredService<ICorrelationIdAccessor>(),
                options.UseServiceToken ? provider.GetRequiredKeyedService<IServiceTokenProvider>(name) : null,
                provider.GetRequiredService<ILogger<JsonApiClient>>());
            return ActivatorUtilities.CreateInstance<TImplementation>(provider, client, context);
        });
        return builder;
    }

    private static bool IsTransient(Outcome<HttpResponseMessage> outcome)
        => outcome.Exception is HttpRequestException or TimeoutRejectedException
            || outcome.Result is { } response && ((int)response.StatusCode >= 500 || (int)response.StatusCode is 408 or 429);

    private sealed record ClientRegistration(string Name);

    private sealed class EmptyCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public Guid? GetCorrelationId() => null;
    }

    private sealed class ClientOptionsValidator(string name, bool isInternal, IServiceProviderIsKeyedService keyedServices)
        : IValidateOptions<HttpApiClientOptions>
    {
        public ValidateOptionsResult Validate(string? optionsName, HttpApiClientOptions options)
        {
            if (optionsName != name)
            {
                return ValidateOptionsResult.Skip;
            }

            var errors = OptionsValidation.Errors(name, options, isInternal).ToList();
            if (options.UseServiceToken && !keyedServices.IsKeyedService(typeof(IServiceTokenProvider), name))
            {
                errors.Add("UseServiceToken requires an IServiceTokenProvider registered with this dependency name.");
            }

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
