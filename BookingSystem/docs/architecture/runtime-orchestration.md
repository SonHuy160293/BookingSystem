# Runtime Orchestration, Aspire, and Observability

## 1. AppHost responsibility

`src/BookingSystem.AppHost/` defines the local runtime topology. It is a composition root, not a business layer.

Its current responsibilities include:

- persistent SQL Server for Identity and PostgreSQL 17 for Cinema;
- the actual module databases;
- each module's `MigrationRunner`;
- each API;
- the YARP gateway, with references to Identity and Cinema HTTP endpoints only.

Identity's `BookingSystem.Identity.Worker` is a class library registered inside
the Identity API process. Its cleanup service shares that API's lifetime and
configuration; it is not an AppHost resource or separate container. See the
[Worker README](../../src/Modules/BookingSystem.Identity/BookingSystem.Identity.Worker/README.md).

Start local orchestration with:

```bash
dotnet run --project src/BookingSystem.AppHost
```

Aspire uses the container runtime for SQL Server and PostgreSQL and provides local observability through its Dashboard.

## 2. Migration ownership

`MigrationRunner` remains the owner of EF Core schema migrations.

AppHost starts the database and waits for each relevant migration runner to complete successfully before starting its API.

Do not move migrations into API startup. The gateway waits for both APIs and has no database or migration references. AppHost injects shared JWT signing-key, issuer and audience parameters into all three hosts. See [gateway operations](../development/gateway.md).

## 3. ServiceDefaults and Observability

`src/BuildingBlocks/BookingSystem.ServiceDefaults/` owns development host concerns such as:

- health endpoints;
- service discovery;
- outgoing `HttpClient` resilience.

YARP uses its own forwarding handler with connection, inactivity and total request budgets; ServiceDefaults HttpClient resilience does not add forwarding retries.

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
- Worker class libraries, including Identity.Worker, use Application and shared
  abstractions; they do not reference Infrastructure, API, or host-only defaults.
- AppHost may reference executable projects.

Do not add resources merely because Aspire supports them. When a real dependency is introduced, model it in AppHost and wire only its actual consumers.

## 5. Telemetry model

Use `ILogger`, `ActivitySource`, and OpenTelemetry for correlated logs, traces, and metrics.

Do not create a separate tracing framework.

Do not emit credentials, tokens, passwords, or raw personal data into telemetry.

The CQRS pipeline order defined in `ARCHITECTURE.md` remains unchanged.

Order and Payment now have booking/payment persistence models and local reliability
tables. Their currently disabled AppHost resources remain disabled. Broker publishers,
consumers, provider adapters, expiration/reconciliation workers, notification delivery
and Redis caching require separate runtime work. See [booking reliability](../development/booking-reliability.md).

## 6. Runtime legibility for agents

Where practical, prefer local runtime tooling that lets an agent inspect the same signals a developer would use to diagnose behavior: logs, traces, metrics, health state, and service dependencies.

Runtime observability should support verification of concrete acceptance criteria, not merely produce dashboards for humans.
