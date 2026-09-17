# Reusable typed HTTP clients

## Approved outcome

Add BookingSystem.HttpClients with a shared JSON transport, separate internal and
external typed-client registration, per-dependency pooling/resilience, typed failure
results, service-token provider hooks and existing correlation propagation.
Add an optional ASP.NET Core correlation adapter and safe telemetry integration.
Existing business-module clients remain unchanged.

## Milestones

- [x] Add shared contracts, options and typed registration.
- [x] Implement request construction, authentication, deadlines and result handling.
- [x] Configure Polly retries/circuit breaker, connection pooling and safe logging.
- [x] Add correlation adapter, documentation and source/diff review.

## Decisions and boundaries

JSON and empty bodies only. GET/HEAD retry; writes have one attempt. No token
acquisition implementation, uploads, streaming API, module adoption, API changes
or persistence changes. ServiceDefaults already adds resilience and service discovery;
new registrations replace only resilience and preserve discovery. HttpClient.Timeout
is infinite; the transport owns a total deadline including response consumption.

## Verification

Acceptance scenarios are documented only. No test files, restore, build, test,
formatter, Docker execution, migration or verification loop. Finish with source/diff
review; compilation and runtime behavior remain unverified.

## Risks

Pooled handlers must not retain request-scoped state. Credentials must never follow
redirects or absolute URL overrides. Avoid stacked retries and unsafe-method replay.
Factory and resilience logging must not leak URLs, credentials or exceptions.
The ASP.NET Core project reference requires a Docker restore-stage project copy.

## Implementation notes and completion

Added the shared project and named typed factories, option validation, keyed token
hooks, per-call credentials/correlation, escaped request construction, result types
and buffered JSON handling. The outer deadline includes token-provider waiting and
response consumption. Caller cancellation takes precedence over failure mapping.
One custom Polly pipeline replaces inherited resilience, preserving discovery;
GET/HEAD alone retry and discarded retry responses are disposed. SocketsHttpHandler
owns pooled connections with configured renewal and handler recycling.

Factory loggers are removed, Polly telemetry uses a null logger factory, and shared
OTLP/local Serilog privacy rules support a reviewed completion template. The optional
ASP.NET Core adapter reads existing TraceIdentifier per call. Solution/reference and
Docker project-copy wiring are included without registering any business client.

Source review covered contracts, URI/header guards, cancellation/deadline boundaries,
retry predicates/disposal, pooling isolation, logging privacy and project references.
Package API documentation and official Polly implementation were inspected without
executing the project. No restore/build/test/formatter/Docker/migration or verification
loop ran; no test files were added. Runtime and compilation validation remain deferred.
