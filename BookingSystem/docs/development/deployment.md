# Deployment and Environment Guidance

## Docker Compose

The application stack in deploy/compose/docker-compose.yaml runs Identity (api),
its migrationrunner and SQL Server, Cinema's PostgreSQL 17, cinema-migrationrunner
and cinema-api, and api-gateway. Existing Identity service names and volumes remain
unchanged. Each API waits for database health and its own migration runner's
successful completion. The gateway waits for both APIs.

Host ports are Identity 8080, Cinema 8081, gateway 8082, SQL Server 1433 and
PostgreSQL 5432. Data volumes are sqlserver-data, api-uploads and cinema-postgres-data.

Copy deploy/env/.dev.env.example, .cinema.dev.env.example and .gateway.dev.env.example
to their local counterparts and replace placeholders. JWT signing key, issuer and
audience must match across all three. Cinema's Identity integration key must match
Identity's configured integration keys. PostgreSQL connection-string and initialization
passwords must agree; environment changes do not reset passwords in an existing volume.

The default application network uses 172.30.50.0/24, with gateway 172.30.50.10.
Override GATEWAY_NETWORK_SUBNET, GATEWAY_NETWORK_DYNAMIC_RANGE and GATEWAY_IPV4 through Compose interpolation when
that range conflicts. The default dynamic range is 172.30.50.128/25. Keep the gateway IP
within the selected subnet and outside its dynamic range. Recreate the
network and application containers after changing addresses, retaining volumes.
Upstream aliases live only on the default network so observability overlays preserve
the trusted forwarding source. Both modules trust the configured gateway IP.

Docker targets are final-identity-api, final-identity-migration, final-cinema-api,
final-cinema-migration and final-gateway. Legacy final-api / final-migration remain
Identity aliases. The gateway image runs as the .NET non-root application user.

The infrastructure-only and observability stacks remain supported. Observability
overlays use the actual api, migrationrunner, cinema-api, cinema-migrationrunner
and api-gateway service names. Telemetry is never a startup/readiness dependency.
See [gateway configuration](gateway.md), [README](../../README.md) and
[observability operations](../../deploy/observability/README.md).

## Environment configuration

Real local env files and credentials must not be committed. Treat .prod.env as
read-only unless explicitly requested. Production HTTPS ingress, hosts, CORS and
trusted proxies must be supplied explicitly; local defaults are development-only.

MigrationRunner remains the schema owner. Gateway changes add no schema migration.
Existing Cinema schema changes must be deployed separately before their APIs work.

## Scope and references

Deployment changes are intentional architecture/operations work. Do not change
migration ownership or introduce runtime dependencies incidentally.

docs/references contains examples rather than implementation truth; compare them
with current source before applying them.
