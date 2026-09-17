# BookingSystem local observability

APIs and MigrationRunners send OTLP/gRPC to the Collector. The Collector sends logs to Loki's native OTLP HTTP endpoint, traces to Tempo, and exposes metrics on its internal port 8889 for Prometheus. Grafana queries those backends. Only the Collector's ingestion ports and Grafana's UI are published, on loopback.

Start the telemetry stack from the repository root:

```powershell
docker compose -f deploy/compose/docker-compose.observability.yaml up -d
```

Open [BookingSystem overview](http://localhost:3000/d/bookingsystem-overview/bookingsystem-overview). Anonymous access is Viewer. Local administrator login is `admin` with `local-observability`, or set `GRAFANA_ADMIN_PASSWORD` before the first startup. These settings are for local development.

Run the Identity API and its migration owner in Docker with the stack:

```powershell
docker compose -f deploy/compose/docker-compose.yaml -f deploy/compose/docker-compose.observability.yaml -f deploy/compose/docker-compose.observability.override.yaml up -d --build
```

The existing `deploy/env/.dev.env` must contain the application's development configuration. SQL Server and migration completion remain application dependencies; telemetry backends are never startup/readiness dependencies. No production environment files are modified.

For Aspire, start the telemetry stack first, then run `dotnet run --project src/BookingSystem.AppHost`. AppHost supplies `http://localhost:4317` to its currently active Identity API and MigrationRunner, overriding Aspire's dashboard OTLP endpoint. Set AppHost's `Observability:Endpoint` if using another Collector address. Other modules remain disabled in AppHost as they were before this change. All five APIs use the same telemetry registration, and all five MigrationRunners initialize and dispose the telemetry providers.

For hosts started directly, use `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`. Inside the combined Compose stack it is `http://otel-collector:4317`. Export uses gRPC for every signal through one endpoint; per-signal endpoints/protocols and `OTEL_RESOURCE_ATTRIBUTES` are intentionally not used. This prevents bypassing the Collector and unrestricted resource attributes. ServiceDefaults calls `AddBookingSystemObservability`; it continues to own health, discovery, and HTTP resilience.

## Options

Configuration keys use the `Observability` section (`__` separators in environment variables):

| Option | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `true` | Disable all telemetry registration if needed |
| `Endpoint` | `http://localhost:4317` | Collector endpoint; `OTEL_EXPORTER_OTLP_ENDPOINT` takes precedence |
| `ServiceName` | Host application name | Stable identity; `OTEL_SERVICE_NAME` takes precedence |
| `ServiceVersion` | unset | Optional release version |
| `TraceSamplingRatio` | `1` | Parent-based sampling; use 100% locally for correlation checks |
| `ExportTimeoutMilliseconds` | `3000` | Bounded exporter timeout (500–10000 ms) |
| `MetricExportIntervalMilliseconds` | `10000` | Metric export interval (1000–60000 ms) |

Logging/tracing use batch export with bounded queues. Collector queues/retries and memory limits are bounded too. Outages can lose telemetry after queues fill; they do not block API availability. Providers are disposed on host shutdown to flush pending signals. Do not add telemetry health checks to application readiness.

## Privacy and correlation

There is one centralized logging path: `ILogger`/Serilog → OpenTelemetry logging provider → Collector → Loki. Keep Serilog's provider forwarding enabled. Do not add an OTLP Serilog sink, file tailing, or Docker log scraping. Console/file sinks remain local and retain their existing structured conventions.

Centralized logs export reviewed CQRS templates and a safe HTTP completion body. Other log bodies become `Application log`. Attributes are restricted to request type, elapsed time, status code, and a validated correlation GUID; exception details, arbitrary rendered messages, scopes, URLs, and payloads are excluded. Local console/file outputs retain their current behavior. Extend the export allowlist only after reviewing a template and its values for privacy.

Trace IDs and span IDs are structured metadata in Loki, never indexed stream labels. Index labels are service, namespace, and environment. Trace tags and metric dimensions exclude raw URLs, SQL statements/parameters, credentials, tokens, request bodies, user IDs, correlation IDs, host/instance IDs, and exception messages. CQRS dimensions use finite request type and outcome (`success`, `failure`, `cancelled`). A returned failed `Result` and thrown exception both count as failure. CQRS telemetry covers requests reaching the existing tracing behavior; validation failures remain visible in HTTP metrics.

In Grafana, expand a log row and choose **View trace**. In the trace view, use the span's **Logs** and **Metrics** links. Trace-to-logs filters Loki's `trace_id` metadata and service label. Trace-to-metrics queries HTTP and CQRS metrics using the service label. HTTP and CQRS duration histograms export trace-based exemplars, and Prometheus exemplars link to Tempo. No Tempo metrics-generator is configured.

The overview includes HTTP rate, latency, 5xx rate, CQRS rate/outcomes/latency, runtime GC/heap metrics, Collector scrape availability, logs, and recent traces. Health traces are filtered. Loki, Tempo, and Prometheus retain seven days of local data in named volumes.

## Focused validation

Use an accessible request path that returns 200 and runs through Serilog request logging:

```powershell
./scripts/smoke-observability.ps1 -ApiUrl http://localhost:8080 -ServiceName BookingSystem.Identity.API -CheckOutage
```

The default path is `/swagger/v1/swagger.json`; use `-RequestPath` for another endpoint. The script checks one centralized request log, matching trace/span IDs, HTTP metrics and a matching trace exemplar, excluded synthetic token data, provisioned dashboard/data sources, and correlation settings. `-CheckOutage` briefly stops and restarts this stack's Collector and verifies API availability. It does not change SQL Server or application containers.

```powershell
dotnet test tests/BookingSystem.Observability.Tests -c Release
dotnet test tests/BookingSystem.SharedKernel.UnitTests -c Release
dotnet test tests/BookingSystem.ArchitectureTests -c Release
```

These targeted checks cover export privacy, duplicate registration/export, Collector outage behavior, CQRS failure telemetry, and project boundaries. They do not invoke the full verification loop.

References: [Loki native OTLP ingestion](https://grafana.com/docs/loki/latest/send-data/otel/), [Tempo data-source provisioning](https://grafana.com/docs/grafana/latest/datasources/tempo/configure-tempo-data-source/).
