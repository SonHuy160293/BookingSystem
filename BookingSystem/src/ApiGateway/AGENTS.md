# API Gateway rules

Root AGENTS.md applies. BookingSystem.ApiGateway is an executable HTTP edge host.

- Reference shared HTTP/response primitives and host defaults only; never reference
  module Application, Domain, Infrastructure, DbContexts, repositories or CQRS.
- Keep explicit service/resource route allowlists. Integration, downstream health,
  Swagger and static-file endpoints are not public proxy routes.
- Gateway JWT checks supplement downstream authorization. Do not turn headers into
  trusted user/tenant claims or move business permissions into this host.
- Preserve upstream status/body, including empty errors. Only gateway-owned failures
  receive gateway envelopes. Never append JSON after a response has started.
- Keep correlation/trace propagation and reviewed request log templates. No raw URLs,
  queries, payloads, credentials, cookie values, tokens or exception messages in logs.
- No forwarding retries or response buffering. Rate limits are per gateway instance.
- Use trusted forwarding addresses and default-network-only Compose upstream aliases.
- Runtime policies and route configuration are captured at startup; restart to change
  them. Service-discovery destination updates remain supported.
- Respect the user's verification mode; no test/build loop unless requested.

See ../../docs/development/gateway.md for configuration, deployment and acceptance.
