# BookingSystem

BookingSystem is a .NET 10 modular backend with Identity, Order, Cinema, Payment, and Cart modules. Each module has its own database and EF Core MigrationRunner. Cinema uses PostgreSQL; the other modules use SQL Server.

## Run locally with Aspire

Install the SDK selected by `global.json` and run a Docker-compatible container runtime. From the repository root:

```powershell
dotnet user-secrets set "Parameters:sqlserver-password" "<strong-local-password>" --project src/BookingSystem.AppHost
dotnet user-secrets set "Parameters:postgres-password" "<strong-cinema-password>" --project src/BookingSystem.AppHost
dotnet run --project src/BookingSystem.AppHost
```

Set the SQL Server password once before the first run. Keep the same secret while reusing the Aspire data volume; SQL Server stores its initialized `sa` password in that volume.

The AppHost currently starts Identity with SQL Server and Cinema with PostgreSQL 17. Each database has a persistent volume. It waits for database health, runs the owning MigrationRunner, and starts the API after migrations succeed. Order, Payment, and Cart definitions remain disabled. Aspire supplies connection strings through `ApplicationDb`. Keep database passwords consistent when reusing initialized volumes. The Dashboard URL and access token appear in the AppHost console output.

The APIs retain `/health/live` and `/health/ready`. In development they also expose Aspire's `/alive` (liveness) and `/health` (readiness, including the database check). Application traces include CQRS, SQL Server, and PostgreSQL spans. The Serilog configuration continues to write console and file logs and forwards structured events to the OpenTelemetry provider; when the AppHost supplies an OTLP endpoint, the provider sends them to the Dashboard.

SQL Server data is stored in an Aspire-managed volume and survives AppHost restarts. To connect to an existing external SQL Server, run each API and MigrationRunner directly with its `ConnectionStrings__ApplicationDb` set to that server instead of starting the AppHost's SQL container. Do not run the Aspire SQL container and the Compose SQL container on the same fixed host port at the same time.

## Existing Docker Compose path

The Compose files under `deploy/compose/` remain supported. Identity uses `deploy/env/.dev.env`; Cinema uses `deploy/env/.cinema.dev.env`. Copy the examples if these local files do not exist, and replace placeholder secrets. Keep `POSTGRES_PASSWORD` and the password in Cinema's `ConnectionStrings__ApplicationDb` identical. Then run:

```powershell
docker compose -f deploy/compose/docker-compose.yaml up --build
```

For SQL Server and PostgreSQL infrastructure alone:

```powershell
docker compose -f deploy/compose/docker-compose.infrastructure.yaml up -d
```

Docker runs the containers. Aspire defines the local application topology and provides startup ordering, connection wiring, and observability; it uses the container runtime for SQL Server and PostgreSQL.

Compose services are `identity-api`, `identity-migrationrunner`, `sqlserver`, `cinema-api`, `cinema-migrationrunner`, and `cinema-postgres`. Identity is available at `http://localhost:8080`, Cinema at `http://localhost:8081`, and PostgreSQL at `localhost:5432`. Container names are module-specific. Identity's `sqlserver-data` and `api-uploads` volumes are preserved; Cinema uses `cinema-postgres-data`.

To start only Cinema:

```powershell
docker compose -f deploy/compose/docker-compose.yaml up -d --build cinema-api
```

When upgrading an already-running stack, retire the previous generic Compose services `api` and `migrationrunner` before starting the renamed Identity services: the Identity API container name and port remain the same. Stop/remove those two old application containers using the previous Compose configuration, keeping the database and upload volumes. Avoid `down -v` unless you intend to delete data.

Cinema has no business entities yet. Its migration runner owns schema updates; create future PostgreSQL migrations using Cinema Infrastructure as the project and Cinema MigrationRunner as the startup project. Existing SQL Server data is not automatically converted to PostgreSQL.
