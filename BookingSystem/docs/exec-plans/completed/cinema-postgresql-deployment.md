# Cinema PostgreSQL deployment

Give Cinema independent Docker API, migration-runner, and PostgreSQL services. Identity retains SQL Server, its data volume and upload volume, and host API port 8080. Cinema uses API port 8081 and PostgreSQL port 5432. Rename generic application services to `identity-api` and `identity-migrationrunner` and update telemetry overlays.

Switch Cinema EF Core and raw SQL connections to Npgsql, update retries and timestamp mappings, and wire Cinema to PostgreSQL in Aspire. Preserve existing user edits and local/production secrets. There are no Cinema entities or migrations yet; no existing database data is converted.

Verification: build AppHost and dependencies, run architecture and observability tests, validate all Compose combinations, build Cinema Docker targets, run Cinema against a fresh isolated PostgreSQL database and verify migration completion and API readiness. Avoid starting or replacing existing Identity containers during verification.

Progress: completed.

- Cinema EF Core uses Npgsql, raw SQL connections use `NpgsqlConnection`, audit columns use PostgreSQL timestamps, retries/configuration and health messages match the provider.
- Identity and Cinema have separate API/migration Docker targets and Compose services. Cinema has PostgreSQL 17 and a dedicated data volume; Identity volumes remain unchanged. Existing Identity Docker targets remain compatibility aliases.
- Added the Cinema environment example and generated an ignored local development file with matching database credentials. Existing Identity and production environment files were preserved.
- Aspire now starts Cinema with PostgreSQL and waits for its migration runner. Telemetry overlays cover both modules, and shared tracing subscribes to Npgsql.
- AppHost and dependencies build with zero errors (existing analyzer/Aspire warnings remain). Architecture tests: 3 passed; observability tests: 1 passed.
- Application, combined-observability, external-observability, and infrastructure Compose configurations validate.
- All four module Docker targets build. A fresh isolated PostgreSQL test reached database health, migration-runner exit 0, and API readiness. Corrected health response reports `PostgreSQL is reachable`. Added the runtime Kerberos library and verified no missing-library warning remains. Removed isolated test containers, network, and temporary database data afterward.
- Central package declarations are unique, all solution project paths resolve, the Cinema provider scan finds no SQL Server remnants, and `git diff --check` passes.

Limitations: Cinema currently has no business entities or migrations; the runner validates the provider and initializes EF migration history. Existing SQL Server data is not converted. Existing Identity containers were not restarted. Deployments using the previous generic application service names need the documented one-time retirement of those application containers.

Sources of truth: [deployment](../../development/deployment.md), [runtime orchestration](../../architecture/runtime-orchestration.md), [verification](../../development/verification.md).

Rollback: revert this provider and deployment change, retaining unrelated working-tree edits. Preserve database volumes; SQL Server data is not migrated automatically.
