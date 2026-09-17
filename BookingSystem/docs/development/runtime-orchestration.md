# Runtime Orchestration, Aspire, and Observability

## 1. AppHost responsibility

`src/BookingSystem.AppHost/` defines the local runtime topology. It is a composition root, not a business layer.

Its current responsibilities include:

- one persistent SQL Server resource;
- the actual module databases;
- each module's `MigrationRunner`;
- each API.

Start local orchestration with:

```bash
dotnet run --project src/BookingSystem.AppHost
```

Aspire uses the container runtime for SQL Server and provides local observability through its Dashboard.

## 2. Migration ownership

`MigrationRunner` remains the owner of EF Core schema migrations.

AppHost starts the database and waits for each relevant migration runner to complete successfully before starting its API.

Do not move migrations into API startup.

## 3. ServiceDefaults and Observability

`src/BuildingBlocks/BookingSystem.ServiceDefaults/` owns development host concerns such as:

- health endpoints;
- service discovery;
- outgoing `HttpClient` resilience.

It delegates telemetry registration to `src/BuildingBlocks/BookingSystem.Observability/`.

Observability owns reusable host-only OpenTelemetry concerns such as:

- resource configuration;
- instrumentation;
- OTLP export;
- privacy filters.

Keep domain models, `Result<T>`, DTOs, commands, queries, repositories, and business services out of ServiceDefaults and Observability.

## 4. Dependency boundaries

- Domain, Application, and Contracts must not reference Aspire, ServiceDefaults, or Observability.
- Infrastructure must not reference AppHost.
- Executable API and Worker projects may reference ServiceDefaults.
- AppHost may reference executable projects.

Do not add resources merely because Aspire supports them. When a real dependency is introduced, model it in AppHost and wire only its actual consumers.

## 5. Telemetry model

Use `ILogger`, `ActivitySource`, and OpenTelemetry for correlated logs, traces, and metrics.

Do not create a separate tracing framework.

Do not emit credentials, tokens, passwords, or raw personal data into telemetry.

The CQRS pipeline order defined in `ARCHITECTURE.md` remains unchanged.

## 6. Runtime legibility for agents

Where practical, prefer local runtime tooling that lets an agent inspect the same signals a developer would use to diagnose behavior: logs, traces, metrics, health state, and service dependencies.

Runtime observability should support verification of concrete acceptance criteria, not merely produce dashboards for humans.
