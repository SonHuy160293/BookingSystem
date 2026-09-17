# Reusable typed HTTP clients

## Ownership and delivery

`BookingSystem.HttpClients` is a .NET 10 BuildingBlock with no module, EF Core,
ASP.NET Core or ServiceDefaults project dependencies. It uses IHttpClientFactory
and Microsoft.Extensions.Http.Resilience 10.10.0 (Polly). No legacy Http.Polly
package is needed. Existing module clients, including IdentityCinemaClient,
have not been migrated.

Application owns business-facing integration interfaces. Infrastructure references
the building block and implements those interfaces with a typed client derived
from JsonApiClient. API composition roots register the dependency. Each dependency
name and interface is registered once, using a separate factory client/pool and
resilience pipeline. Typed clients are transient; do not capture one in a singleton.
Workers create a scope per operation and resolve their typed clients in that scope.

## Internal typed client example

The following is usage documentation only; no example business client is registered
by this change. The client constructor receives HttpClient and HttpApiClientContext;
other ordinary DI constructor dependencies are also supported.

```csharp
// Application interface (DTOs stay in the owning module or shared Contracts).
public interface IBranchDirectory
{
    Task<HttpCallResult<BranchDto>> GetAsync(Guid id, CancellationToken cancellationToken);
}

// Infrastructure implementation.
public sealed class BranchDirectory(HttpClient client, HttpApiClientContext context)
    : JsonApiClient(client, context), IBranchDirectory
{
    public Task<HttpCallResult<BranchDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => SendAsync<BranchDto>(new HttpApiRequest
        {
            Method = HttpMethod.Get,
            Path = "api/branches/{id}",
            RouteValues = new Dictionary<string, string> { ["id"] = id.ToString("D") },
            QueryParameters = [new("include", "address"), new("include", "contact")]
        }, cancellationToken);
}

// API composition root. Call ServiceDefaults before typed-client registration.
builder.AddServiceDefaults();
builder.Services.AddHttpClientCorrelation();
builder.Services.AddInternalApiClient<IBranchDirectory, BranchDirectory>(
    "IdentityDirectory", builder.Configuration.GetSection("HttpClients:IdentityDirectory"));
```

If the upstream uses the BookingSystem envelope, request
`HttpCallResult<ApiResponse<BranchDto>>` instead. The transport deserializes exactly
the requested type and does not unwrap envelopes or interpret business-level errors.
Business clients decide whether 404 means a missing resource, an empty result or an
application error. CancellationToken is required on both send overloads.

```json
{
  "HttpClients": {
    "IdentityDirectory": {
      "BaseAddress": "http://identity-api/",
      "IntegrationKey": "<supply-through-secret-configuration>",
      "UseServiceToken": false,
      "TotalTimeout": "00:00:30",
      "AttemptTimeout": "00:00:10",
      "ConnectTimeout": "00:00:05"
    }
  }
}
```

Use a configured HTTP(S) logical service address with ServiceDefaults, or a normal
DNS address without service discovery. The registration preserves inherited
discovery handlers but does not register discovery itself. The integration key is
always sent as X-Integration-Key; internal registrations reject external API-key
settings. Never commit real keys into example configuration.

## External authentication and service tokens

```csharp
builder.Services.AddExternalApiClient<IWeatherDirectory, WeatherDirectory>(
    "Weather", builder.Configuration.GetSection("HttpClients:Weather"));

// Optional, for either an internal or external dependency:
builder.Services.AddKeyedSingleton<IServiceTokenProvider, WeatherServiceTokenProvider>("Weather");
```

External options use BaseAddress, optional ApiKeyHeaderName/ApiKey, UseServiceToken
and ForwardCorrelationId. Both authentication methods are optional and may be used
together. External clients reject IntegrationKey. External API-key header names
cannot be reserved headers such as Authorization or X-Integration-Key.

UseServiceToken=true requires an IServiceTokenProvider registered under exactly
the dependency name. Missing registrations fail startup options validation. The
provider implements:

```csharp
ValueTask<string?> GetAccessTokenAsync(string dependencyName, CancellationToken cancellationToken);
```

Providers must be singleton-safe, must honor cancellation and must not capture
request-scoped state. They own service-token acquisition, caching and renewal.
The transport bounds the wait by the total deadline, validates a nonempty token,
and sets Authorization: Bearer per request. It never forwards an incoming user's
Authorization header. Provider errors return AuthenticationFailure without raw
exception details. No OAuth/token endpoint implementation is included.

## Request, response and correlation rules

HttpApiRequest has Method, Path, RouteValues, QueryParameters, Headers, optional
Body and optional Guid CorrelationId. Supply a relative path without a leading
slash, query, fragment, backslash or authority. Placeholder names use letters,
digits and underscores, starting with a letter; all placeholders and supplied
route keys must match. Values are escaped individually. Repeated query names are
supported, null values are omitted, and empty values are preserved.

Body is serialized as JSON using JsonOptions; null means no request body. Configure
serializer options in code through named options, for example:

```csharp
builder.Services.Configure<HttpApiClientOptions>("Weather", options =>
    options.JsonOptions.PropertyNameCaseInsensitive = true);
```

Headers are per call. Reserved authentication, correlation, tracing, cookie and
transport/content headers cannot be overridden. Redirects and cookies are disabled.
No mutable DefaultRequestHeaders state is used. Callers must not re-enable unsafe
behavior through additional IHttpClientBuilder customization.

Internal clients forward explicit CorrelationId first, then the accessor's current
ID. Empty explicit GUIDs are rejected; absent IDs are omitted, never generated.
External clients omit this header unless ForwardCorrelationId is enabled.
AddHttpClientCorrelation registers an optional ASP.NET Core adapter that reads
HttpContext.TraceIdentifier during each call after UseCorrelationId middleware.
Background operations can supply CorrelationId directly or register their own
thread-safe ICorrelationIdAccessor. No pooled handler retains a request context.

All 2xx statuses succeed. Empty content (including 204) yields HasBody=false.
JSON null yields HasBody=true with null Data. A nonempty body must be JSON for the
generic overload; application/json and +json media types are supported, and absent
Content-Type is allowed if the body is valid JSON. Malformed JSON or a non-JSON
media type yields InvalidResponse. The nongeneric overload accepts any response
body, consumes it and discards it. Request/response messages are disposed per call.
This is a buffered JSON API abstraction, not an upload/download streaming API.

HttpCallResult and HttpCallResult<T> expose IsSuccess, nullable StatusCode,
HasBody and Failure; the generic type also exposes Data. Non-success HTTP bodies
are not returned or logged. Failure contains a fixed Code/Message, Kind and optional
RetryAfter. StatusCode remains available if headers arrived before a later failure.
HasBody describes consumed content; error bodies are intentionally not consumed,
so an HttpFailure does not claim that the upstream error payload was read.

| Condition | Failure kind / behavior |
| --- | --- |
| Non-2xx, including redirects and 404/409 | HttpFailure; preserve status and valid Retry-After |
| DNS, TLS, connection or body transport failure | NetworkFailure |
| Attempt timeout or total deadline | Timeout |
| Open circuit | CircuitOpen |
| Invalid JSON / response representation | InvalidResponse |
| Failed or empty service token | AuthenticationFailure |
| Caller cancellation | Throw OperationCanceledException |
| Invalid request, serialization or configuration | Programming/configuration exception |

Result codes use HttpDependency.<Kind>. No failures become successful fallback data.

## Pooling, deadlines and resilience

| Named option | Default |
| --- | --- |
| TotalTimeout | 30 seconds |
| AttemptTimeout | 10 seconds |
| ConnectTimeout | 5 seconds |
| HandlerLifetime | 5 minutes |
| PooledConnectionLifetime | 2 minutes |
| PooledConnectionIdleTimeout | 1 minute |
| MaxConnectionsPerServer | 64 |
| MaxRetryAttempts | 2 additional attempts; zero disables retries |
| RetryDelay / MaxRetryDelay | 200 milliseconds / 2 seconds |
| UseRetryJitter | true |
| CircuitFailureRatio / CircuitMinimumThroughput | 0.5 / 20 |
| CircuitSamplingDuration / CircuitBreakDuration | 30 seconds / 30 seconds |

Durations are configurable TimeSpan strings. Startup validation requires HTTP(S)
base addresses without userinfo/query/fragment, valid credentials/header names,
positive pooling/connection limits, Connect <= Attempt <= Total, retry bounds,
and CircuitSamplingDuration >= 2 * AttemptTimeout. Timeout, pooling and circuit
durations are bounded to one second through one day. Options are captured when
clients/handlers/pipelines are created; live credential or settings reload is not
a supported contract. Reconfigure through restart, or implement token refresh in
the token provider. Factory handler recycling and pooled connection lifetime allow
new connections to resolve DNS again; in-flight connections are not forcibly cut.

HttpClient.Timeout is infinite. JsonApiClient owns the total deadline, including
request preparation, token acquisition, retry waits, response buffering and JSON
deserialization. CancellationToken reaches token providers, HTTP sends and reads;
the deadline is also checked after serialization/deserialization. Synchronous JSON
CPU work is not preempted. Polly's attempt timeout covers sending through response
headers; the outer deadline also covers consuming the response body.

The client removes inherited resilience handlers and adds exactly one custom
retry -> circuit breaker -> attempt timeout pipeline. It retains service discovery.
Retries apply only to GET/HEAD on HttpRequestException, attempt timeout, 408, 429
and 5xx. Other methods have one attempt, even when an Idempotency-Key is supplied.
Jittered exponential backoff is capped by MaxRetryDelay; valid Retry-After takes
precedence and waits remain cancellable within the total deadline. Discarded retry
responses are disposed. Caller cancellation and circuit-open rejection are not retried.

The circuit counts transient failures for all methods and is shared by callers of
the same dependency within one process. Failures of one dependency do not open
another dependency's circuit. No hedging, distributed breaker or automatic token
refresh/retry-on-401 is added. Provider idempotency remains a business integration
responsibility for future write retry support.

## Observability and later acceptance

Existing ServiceDefaults HttpClient instrumentation supplies W3C tracing. Do not
copy traceparent/tracestate manually or add a second instrumentation registration.
Existing privacy processors remove raw URL/host/query tags from exported traces.
Default factory request loggers are removed for these clients; Polly uses a null
logger factory and no HTTP enrichment of its replacement telemetry listener.
The completion logger emits only dependency name, normalized method, status,
elapsed milliseconds, fixed outcome and normalized correlation ID. Status zero
means no response headers; None means no correlation ID or no failure as appropriate.

Shared OTLP logging allows only the reviewed completion template and safe fields.
AddConfiguredSerilog also applies a targeted enricher/filter to this logger before
local sinks, removing ambient scope properties and rejecting unexpected templates
or exceptions. Custom hosts must apply equivalent local-sink scope protection.
Bodies, URLs, query values, credentials, tokens and provider exception messages
are never passed to the completion logger. Telemetry export is not a dependency
of sending requests.

Future executable acceptance should use a fake HttpMessageHandler via the returned
IHttpClientBuilder.ConfigurePrimaryHttpMessageHandler, plus an interface-based fake
token provider/accessor. Direct tests can construct a derived client with HttpClient
and HttpApiClientContext. Documented scenarios:

- Internal key/correlation propagation and no header leakage between concurrent calls.
- External authentication isolation, optional token provider and absent-correlation behavior.
- Route/query escaping, duplicate query keys, JSON envelopes, JSON null and empty bodies.
- Non-success status preservation, malformed responses and safe failure messages.
- GET/HEAD retry limits, Retry-After, jitter, and single-attempt writes.
- Circuit opening, half-open recovery and isolation by dependency.
- Connect/attempt/total timeout, slow response body and caller cancellation.
- Pooled handler reuse, connection renewal and no duplicate ServiceDefaults pipeline.
- No sensitive values in local completion logs or exported telemetry.

No test files, restore, build, tests, formatter, Docker execution, migrations or
verification loop were run or created for this implementation. Source/diff review
only; compilation and runtime resilience behavior remain unverified.
