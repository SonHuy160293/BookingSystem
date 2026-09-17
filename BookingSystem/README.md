# BookingSystem

BookingSystem is a .NET 10 modular backend with Identity, Order, Inventory, Payment, and Cart modules. Each module has its own SQL Server database and EF Core MigrationRunner.

## Run locally with Aspire

Install the SDK selected by `global.json` and run a Docker-compatible container runtime. From the repository root:

```powershell
dotnet user-secrets set "Parameters:sqlserver-password" "<strong-local-password>" --project src/BookingSystem.AppHost
dotnet run --project src/BookingSystem.AppHost
```

Set the SQL Server password once before the first run. Keep the same secret while reusing the Aspire data volume; SQL Server stores its initialized `sa` password in that volume.

The AppHost starts one persistent SQL Server container, creates five logical database resources, runs each module's MigrationRunner after SQL Server is healthy, and starts each API after its migration succeeds. Aspire provides the database connection strings to each process under its existing `ApplicationDb` key. Aspire's Dashboard URL and access token appear in the AppHost console output. The Dashboard shows resources, endpoints, health, console logs, structured logs, traces, and metrics.

The APIs retain `/health/live` and `/health/ready`. In development they also expose Aspire's `/alive` (liveness) and `/health` (readiness, including the database check). Application traces include the CQRS request span and SQL client spans. The Serilog configuration continues to write console and file logs and forwards structured events to the OpenTelemetry provider; when the AppHost supplies an OTLP endpoint, the provider sends them to the Dashboard.

SQL Server data is stored in an Aspire-managed volume and survives AppHost restarts. To connect to an existing external SQL Server, run each API and MigrationRunner directly with its `ConnectionStrings__ApplicationDb` set to that server instead of starting the AppHost's SQL container. Do not run the Aspire SQL container and the Compose SQL container on the same fixed host port at the same time.

## Existing Docker Compose path

The Compose files under `deploy/compose/` remain supported. For the existing Identity stack, supply local values in `deploy/env/.dev.env`, then run:

```powershell
docker compose -f deploy/compose/docker-compose.yaml up --build
```

For SQL Server infrastructure alone:

```powershell
docker compose -f deploy/compose/docker-compose.infrastructure.yaml up -d
```

Docker runs the containers. Aspire defines the local application topology and provides startup ordering, connection wiring, and observability; it uses the container runtime for SQL Server.
