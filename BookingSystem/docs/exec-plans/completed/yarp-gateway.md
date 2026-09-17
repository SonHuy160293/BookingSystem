# YARP gateway execution plan

## Outcome and scope

Implement the approved .NET 10 BookingSystem.ApiGateway for Identity and Cinema,
with service-prefixed routing, strict JWT authentication for management routes,
CORS/security policies, per-route rate limiting, trace/correlation propagation,
safe logs, bounded upstream timeouts, health checks and gateway-only error envelopes.

Support AppHost and Compose. Keep direct development API ports. Preserve module
authorization, database ownership and MigrationRunner startup dependencies.
No business logic, database access, forwarding retries or distributed limiter.

## Accepted decisions

- Packages: Yarp.ReverseProxy 2.3.0; Microsoft.Extensions.ServiceDiscovery.Yarp 10.10.0.
- Public prefixes /identity and /cinema are removed before forwarding.
- Anonymous POST login/refresh/logout; all management routes require JWT.
- Gateway 8082, Identity 8080, Cinema 8081 in Compose.
- Per-instance, per-route rate buckets: login 5/10 minutes, refresh/logout 60/minute,
  auth management 60/minute, reads 120/minute, writes 30/minute; no queue.
- Connection/idle/total timeouts: 5/15/30 seconds.
- Active readiness probes: /health/ready, 10-second interval, 2-second timeout,
  two consecutive failures. Readiness requires both clusters; liveness is local.
- Compose subnet 172.30.50.0/24 and gateway 172.30.50.10 are overridable.
- Real secrets and production environment files remain untouched.

## Implementation slices

- [x] Gateway project, scoped instructions, packages and configuration.
- [x] Routing, JWT, CORS, transforms and Location rewriting.
- [x] Limiting, timeouts, gateway-only failure handling.
- [x] Correlation, safe telemetry/logging, health endpoints.
- [x] AppHost, Docker, Compose, trusted module forwarding.
- [x] Documentation and source/diff review.

## Verification and acceptance

User explicitly requested no restore, build, test, formatter, Docker execution,
migration or verification loop. Do not add automated test infrastructure.
Acceptance scenarios are documented in the gateway operations guide; they are
not claims of executable validation.

## Risks and rollback

Direct development ports bypass gateway policies. Existing ScreeningRoom schema
deployment remains separate. Rollback removes the gateway from AppHost/Compose
while retaining the original direct APIs and migration runners.

## Progress

- Inspected current AppHost, module hosts, shared middleware/telemetry, Compose and
  Docker targets. Consulted pinned local YARP package API documentation and source.
- Implemented the gateway HTTP pipeline, explicit route groups, strict JWT, CORS,
  forwarding/Location transforms, per-route limits, timeouts and gateway-only errors.
- Added safe local/exported logging, correlation/tracing, streaming body limits and
  active-probe readiness. Identity now registers ServiceDefaults.
- Wired AppHost, Docker, Compose and module trusted forwarding; kept Identity service
  names, direct ports and migration ownership. Reserved a separate dynamic IP range
  so combined observability services cannot take the trusted gateway address.
- Completed source/diff review and operations documentation. No restore, build,
  tests, formatter, Docker execution, migrations or executable verification ran.
- Follow-up: populate local secrets/env files, deploy existing Cinema schema changes,
  then execute the documented scenarios when verification is requested.
