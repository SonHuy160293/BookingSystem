# Deployment and Environment Guidance

## Docker Compose

`deploy/compose/` holds the local Docker Compose stacks, including:

- `docker-compose.yaml`;
- `docker-compose.infrastructure.yaml`;
- `docker-compose.observability.yaml` and its application/network overlays.

`deploy/compose/` remains a supported deployment/local-run path unless an explicit decision changes that contract.

The application stack runs independent `identity-api` and `cinema-api` containers, each gated on its database health and its own successfully completed migration runner. Identity uses SQL Server (`sqlserver`), API host port 8080, and the existing `sqlserver-data`/`api-uploads` volumes. Cinema uses PostgreSQL 17 (`cinema-postgres`), API host port 8081, database host port 5432, and the `cinema-postgres-data` volume.

Identity uses the local `.dev.env`; Cinema uses `.cinema.dev.env`, both under `deploy/env/`. Copy the corresponding `.example` files and replace placeholders. Set Cinema's `POSTGRES_PASSWORD` and connection-string password to the same value; changing environment values does not reset passwords in an initialized PostgreSQL volume. The example connection string uses the Compose service hostname, while running Cinema directly on the host requires `Host=localhost`.

The Dockerfile exposes `final-identity-api`, `final-identity-migration`, `final-cinema-api`, and `final-cinema-migration` targets. The previous `final-api`/`final-migration` targets remain Identity aliases.

Existing deployments must retire the old `api`/`migrationrunner` application services before starting their renamed Identity replacements. Keep existing database and upload volumes. See [README](../../README.md) for startup commands and [observability](../../deploy/observability/README.md) for telemetry overlays.

## Environment configuration

`deploy/env/` holds environment configuration.

Treat `.prod.env` as read-only unless the user explicitly asks to change it.

Do not modify production configuration as a side effect of an unrelated feature task.

If `.prod.env` contains real credentials or secrets, it must not be committed.

## References

`docs/references/` contains sample/reference material when present. It is not part of the build and must not be treated as current implementation without verification against source.

## Scope discipline

Deployment changes should be intentional. Adding a new runtime dependency, changing migration ownership, or replacing a supported deployment path is an architecture/operations decision, not incidental feature work.
