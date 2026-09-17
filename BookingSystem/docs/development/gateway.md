# BookingSystem API gateway

The .NET 10 `BookingSystem.ApiGateway` host uses YARP 2.3.0 and service-discovery
integration 10.10.0. It depends on shared HTTP primitives, response types and
ServiceDefaults. Module databases, permissions, sessions and business logic remain
owned by their modules.

## Public HTTP contract

Only these resource groups are forwarded; the first service prefix is removed.
Each resource has separate read/write route entries for its supported methods.

| Public path | Methods | Gateway authentication |
| --- | --- | --- |
| /identity/api/Auth/login | POST | Anonymous |
| /identity/api/Auth/refresh, /identity/api/Auth/logout | POST | Anonymous |
| /identity/api/Auth/logout-all | POST | JWT |
| /identity/api/Auth/sessions | GET | JWT |
| /identity/api/Auth/sessions/{sessionId:guid} | DELETE | JWT |
| /identity/api/v1/Users/{**remainder} | GET, POST | JWT |
| /identity/api/v1/Roles/{**remainder} | GET, POST | JWT |
| /identity/api/v1/Permissions/{**remainder} | GET | JWT |
| /identity/api/v1/Tenants/{**remainder} | GET, POST, PUT | JWT |
| /identity/api/v1/Branches/{**remainder} | GET, POST, PUT | JWT |
| /cinema/api/v1/Cinemas/{**remainder} | GET, POST, PUT | JWT |
| /cinema/api/v1/Films/{**remainder} | GET, POST, PUT | JWT |
| /cinema/api/v1/ScreeningRooms/{**remainder} | GET, POST, PUT | JWT |
| /health/live, /health/ready | GET | Anonymous; gateway only |

Resource catch-alls include the resource root, IDs and existing child actions.
They never expose a service-wide catch-all. Unsupported methods receive 405
with Allow; unknown resources receive 404. Integration APIs, downstream health,
Swagger and static resources are excluded. Matching paths still obey downstream
endpoint and permission rules.

For example, `/cinema/api/v1/ScreeningRooms` becomes
`/api/v1/ScreeningRooms`. Same-upstream absolute and root-relative /api Location
headers become relative public URLs with the service prefix.

## Configuration

All settings use normal .NET configuration; environment keys use double underscores.
Route and policy changes require a restart. Service discovery can update destination
addresses while the host runs.

| Setting | Default / requirement |
| --- | --- |
| Jwt:SecretKey | Required HS256 key, at least 32 UTF-8 bytes; identical to Identity |
| Jwt:Issuer / Jwt:Audience | Required and validated in every environment |
| Gateway:AllowedHosts | Explicit hosts; Development localhost, 127.0.0.1, ::1 |
| Gateway:KnownProxies | Explicit trusted ingress IPs; empty means forwarding is disabled |
| Gateway:MaxRequestBodyBytes | 10485760 (10 MiB) |
| Gateway:ConnectionTimeoutSeconds | 5 |
| Gateway:ActivityTimeoutSeconds | 15 |
| Gateway:RequestTimeoutSeconds | 30 |
| Cors:AllowedOrigins | Development http://localhost:3000, :4200, :5173; configure explicitly elsewhere |

JWT signature, HS256 algorithm, issuer, audience, expiration, lifetime and nonempty
GUID subject are validated with zero clock skew. The original bearer token reaches
the module for independent validation.

CORS allows Authorization, Content-Type, X-Correlation-Id, traceparent and tracestate;
exposes X-Correlation-Id, Retry-After and Location; disables credentials. The gateway
removes downstream CORS headers. Preflight executes before authentication.

Outside Development, configure explicit public hosts, CORS origins, HTTPS ingress
and trusted ingress IPs. The root ASP.NET AllowedHosts setting is deliberately
permissive; Gateway:AllowedHosts enforces the allowlist inside the error boundary.
Forwarding is limited to one trusted hop. Health probes are exempt from HTTPS
redirection; other HTTP requests redirect to HTTPS. HSTS and security response
headers are enabled. Modules trust loopback and explicitly configured
ForwardedHeaders:KnownProxies; Compose supplies the gateway's fixed source IP.

Inbound forwarding headers are replaced, and integration keys and spoofed
user/tenant/branch/role/permission headers are removed. No tenant permissions are
interpreted at the gateway.

## Limits, timeouts and failures

Rate policies are configurable at Gateway:RateLimits:<policy>:PermitLimit and
WindowSeconds. They use fixed windows with no queue. Buckets contain the policy,
route ID and authenticated subject, or effective client IP for public requests.

| Policy | Requests / window |
| --- | --- |
| GatewayLogin | 5 / 600 seconds |
| GatewayRefresh | 60 / 60 seconds |
| GatewayAuthManagement | 60 / 60 seconds |
| GatewayRead | 120 / 60 seconds |
| GatewayWrite | 30 / 60 seconds |

Limits are per process; multiple gateway instances multiply capacity. Downstream
Identity limits still apply. There are no forwarding retries or request buffering.
Connection, inactivity and total request budgets are separate; streaming is retained.
The body limit is enforced both through server/route limits and a streaming byte counter.

Gateway-generated failures use the shared ApiResponse / ApiErrorResponse envelope,
generic messages, no-store, and the normalized correlation GUID in error TraceId.

| Status | Code |
| --- | --- |
| 400 | Gateway.BadRequest |
| 401 | Gateway.Unauthorized (WWW-Authenticate: Bearer) |
| 403 | Gateway.Forbidden |
| 404 | Gateway.RouteNotFound |
| 405 | Gateway.MethodNotAllowed |
| 413 | Gateway.PayloadTooLarge |
| 429 | Gateway.RateLimited (Retry-After in seconds) |
| 502 | Gateway.BadGateway |
| 503 | Gateway.ServiceUnavailable |
| 504 | Gateway.UpstreamTimeout |
| 500 | Gateway.InternalError |

Actual downstream statuses and bodies, including empty responses, pass through.
Once response headers have started, failures abort the incomplete response.
Client disconnects are logged as Gateway.ClientDisconnected with completion status
499 rather than written as timeout responses.

## Health and telemetry

Liveness checks only the gateway. Readiness requires valid startup configuration
and a healthy destination in both clusters. Active /health/ready probes run every
10 seconds with a 2-second timeout; two consecutive failures mark a destination
unhealthy. Readiness reads YARP health state without starting new probes.
The gateway can initially report not ready until probes succeed.

Shared GUID correlation IDs are normalized on requests and responses. W3C context
propagates through YARP and HTTP instrumentation; Yarp.ReverseProxy spans are
subscribed through the existing OTLP pipeline. Inbound baggage is removed before
forwarding. Identity now registers ServiceDefaults so traces continue downstream.

The gateway uses reviewed static completion templates and an explicit property
allowlist before local Serilog sinks and OTLP export. Only method, route/cluster
IDs, status, duration, correlation/trace/span IDs and fixed failure codes are logged.
Unreviewed properties and framework request logs are rejected. Bodies, raw paths,
queries, tokens, cookies, credentials and exception messages are excluded.
Exporter availability never gates readiness.

## Running with AppHost or Compose

AppHost parameters sqlserver-password and postgres-password remain required.
Set Parameters:jwt-signing-key (secret), Parameters:jwt-issuer and
Parameters:jwt-audience in AppHost user secrets. The same values are injected into
Identity, Cinema and the gateway. AppHost discovers module HTTP endpoints and
waits for both APIs; each API retains its database/migration dependencies.

Compose retains Identity services api and migrationrunner and adds Cinema's
PostgreSQL, migration runner and API, plus api-gateway. Copy the three examples to
local deploy/env/.dev.env, .cinema.dev.env and .gateway.dev.env; replace placeholders.
Use identical JWT values across all three. Cinema's IdentityIntegration:ApiKey must
match an Identity IntegrationApi:ApiKeys entry. Real credentials are never checked in.

| Host port | Service |
| --- | --- |
| 8080 | Identity direct |
| 8081 | Cinema direct |
| 8082 | Gateway |
| 1433 / 5432 | SQL Server / PostgreSQL |

Default subnet is 172.30.50.0/24 and gateway IPv4 is 172.30.50.10. If either conflicts,
set GATEWAY_NETWORK_SUBNET, GATEWAY_NETWORK_DYNAMIC_RANGE and GATEWAY_IPV4 in the shell or Compose interpolation
env file before creating the network. Choose a free gateway address within that
subnet and outside the dynamic allocation range. The default dynamic range is
172.30.50.128/25, keeping dynamic services away from the trusted .10 address.
Choose an equivalent contained range when overriding the subnet.
Changing these requires recreating the application network and containers,
while retaining data volumes. Service env_file entries do not supply Compose
interpolation variables.

Only the application default network carries identity-upstream and cinema-upstream
aliases. Discovery resolves those aliases so forwarding retains the trusted static
gateway source even when the observability external network is attached.
See [deployment](deployment.md) and [observability](../../deploy/observability/README.md).

Direct development ports bypass gateway authentication and rate policies.
ScreeningRoom requires JWT through the gateway; direct access retains the existing
controller behavior. The existing Cinema schema must be deployed independently.

## Acceptance scenarios recorded for later execution

- Both prefixes route correctly and create responses return usable prefixed Locations.
- Anonymous auth requests work; management rejects missing, invalid or expired JWTs.
- ScreeningRoom uses gateway JWT protection while direct development behavior remains.
- Integration, diagnostic and static paths are excluded.
- Configured-origin preflight succeeds without JWT; downstream CORS cannot override it.
- Independent route buckets reject excess requests with 429 and Retry-After.
- Correlation and trace context reach downstream services without untrusted baggage.
- Credentials, bodies, query strings and raw URLs are absent from gateway output.
- Upstream failures use gateway envelopes while downstream error bodies remain intact.
- Liveness survives upstream outages; readiness fails and recovers with cluster health.
- Direct API ports and migration startup ordering remain available.

Implementation received source/diff review only. No restore, build, automated tests,
formatter, Docker execution, migration or verification loop ran. These scenarios
remain unexecuted; no automated test infrastructure was added.
