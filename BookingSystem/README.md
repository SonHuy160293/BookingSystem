# BookingSystem

BookingSystem is a .NET 10 modular backend with Identity, Order, Cinema, Payment, and Cart modules. Each module has its own database and EF Core MigrationRunner. Cinema uses PostgreSQL; the other modules use SQL Server.

## Run locally with Aspire

Install the SDK selected by `global.json` and run a Docker-compatible container runtime. From the repository root:

```powershell
dotnet user-secrets set "Parameters:sqlserver-password" "<strong-local-password>" --project src/BookingSystem.AppHost
dotnet user-secrets set "Parameters:postgres-password" "<strong-cinema-password>" --project src/BookingSystem.AppHost
dotnet user-secrets set "Parameters:jwt-signing-key" "<shared-key-at-least-32-bytes>" --project src/BookingSystem.AppHost
dotnet user-secrets set "Parameters:jwt-issuer" "Company.Project" --project src/BookingSystem.AppHost
dotnet user-secrets set "Parameters:jwt-audience" "Company.Project.Api" --project src/BookingSystem.AppHost
dotnet run --project src/BookingSystem.AppHost
```

Set the SQL Server password once before the first run. Keep the same secret while reusing the Aspire data volume; SQL Server stores its initialized `sa` password in that volume.

The AppHost starts Identity with SQL Server, Cinema with PostgreSQL 17, and the YARP gateway. The gateway discovers both APIs and shares their configured JWT issuer, audience and signing key. Each database has a persistent volume. It waits for database health, runs the owning MigrationRunner, and starts the API after migrations succeed. Order, Payment, and Cart definitions remain disabled. Aspire supplies connection strings through `ApplicationDb`. Keep database passwords consistent when reusing initialized volumes. The Dashboard URL and access token appear in the AppHost console output.

The APIs retain `/health/live` and `/health/ready`. The gateway has independent liveness and readiness requiring both upstream clusters. Application traces include CQRS, SQL Server, and PostgreSQL spans. The Serilog configuration continues to write console and file logs and forwards structured events to the OpenTelemetry provider; when the AppHost supplies an OTLP endpoint, the provider sends them to the Dashboard.

SQL Server data is stored in an Aspire-managed volume and survives AppHost restarts. To connect to an existing external SQL Server, run each API and MigrationRunner directly with its `ConnectionStrings__ApplicationDb` set to that server instead of starting the AppHost's SQL container. Do not run the Aspire SQL container and the Compose SQL container on the same fixed host port at the same time.

## Existing Docker Compose path

The Compose files under `deploy/compose/` remain supported. Identity uses `deploy/env/.dev.env`; Cinema uses `deploy/env/.cinema.dev.env`; the gateway uses `deploy/env/.gateway.dev.env`. Match JWT settings across all three. Copy the examples if these local files do not exist, and replace placeholder secrets. Keep `POSTGRES_PASSWORD` and the password in Cinema's `ConnectionStrings__ApplicationDb` identical. Then run:

```powershell
docker compose -f deploy/compose/docker-compose.yaml up --build
```

For SQL Server and PostgreSQL infrastructure alone:

```powershell
docker compose -f deploy/compose/docker-compose.infrastructure.yaml up -d
```

Docker runs the containers. Aspire defines the local application topology and provides startup ordering, connection wiring, and observability; it uses the container runtime for SQL Server and PostgreSQL.

Compose services are `api` (Identity), `migrationrunner` (Identity), `sqlserver`, `cinema-api`, `cinema-migrationrunner`, `cinema-postgres`, and `api-gateway`. Identity is available at `http://localhost:8080`, Cinema at `http://localhost:8081`, the gateway at `http://localhost:8082`, and PostgreSQL at `localhost:5432`. Identity's `sqlserver-data` and `api-uploads` volumes are preserved; Cinema uses `cinema-postgres-data`.

To start only Cinema:

```powershell
docker compose -f deploy/compose/docker-compose.yaml up -d --build cinema-api
```

Identity retains the existing `api` and `migrationrunner` service names. The gateway uses a configurable private subnet and trusted fixed address; see [gateway configuration](docs/development/gateway.md) for network overrides, route policies and environment requirements.

Cinema's migration runner owns schema updates; create future PostgreSQL migrations using Cinema Infrastructure as the project and Cinema MigrationRunner as the startup project. Existing SQL Server data is not automatically converted to PostgreSQL.
