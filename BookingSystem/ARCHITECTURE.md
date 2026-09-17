# BookingSystem Architecture

This document is the repository-level architecture map. It explains the invariants that root `AGENTS.md` keeps concise and points to more specialized documentation where necessary.

## 1. System shape

BookingSystem is a modular monolith built with ASP.NET Core and Clean Architecture.

Business capabilities live under `src/Modules/<ModuleName>`. A module may contain `Api`, `Application`, `Domain`, `Infrastructure`, `Worker`, and `MigrationRunner` projects as required by that module.

Cross-module contracts live under `src/Contracts/`. Shared technical primitives live under `src/BuildingBlocks/`.

BookingSystem.HttpClients provides typed internal/external JSON clients through
IHttpClientFactory, with per-dependency pooling, Polly resilience, service credential
hooks and explicit failure results. Business integration interfaces remain in
Application and typed implementations in Infrastructure. The building block has
no ASP.NET Core or module references; BookingSystem.AspNetCore supplies the optional
correlation adapter and local-log privacy integration. Existing module clients are
unchanged. See [HTTP client usage](docs/development/http-clients.md).

Database tables use plural PascalCase names and PascalCase columns within their
established schemas. PostgreSQL SQL must quote these identifiers, for example
`cinema."ScreeningRooms"`. Runtime EF mappings follow this convention; existing
deployed lowercase tables need separate rename migrations. Historical migrations
and snapshots retain their recorded names until a future migration updates the schema.

Reusable domain validation primitives live in
`BookingSystem.SharedKernel.Validation.Guard`: validation exceptions, required GUIDs,
bounded text, SHA-256 hexadecimal hashes and defined enum values. Module-local
`ModelValidation` helpers delegate to these primitives and retain business rules,
including currency amounts, ownership, seat rules and accepted code alphabets.
Reliability persistence keeps JSON and lease validation in its own helper.
The shared guards have no EF Core, HTTP or module dependencies; existing validation
messages and normalization policies remain the responsibility of their callers.

Do not infer that every reference/example module exists. Verify source before copying a pattern.

## 2. Dependency law

Allowed repository-level dependency direction:

```text
Module.Api             -> Module.Application + Module.Infrastructure + optional Worker library + Contracts + BuildingBlocks
Module.Worker (library)-> Module.Application + BuildingBlocks
Module.Infrastructure  -> Module.Application + Module.Domain + Contracts + BuildingBlocks
Module.Application     -> Module.Domain + Contracts + BuildingBlocks
Module.Domain          -> BuildingBlocks only
Contracts              -> BuildingBlocks only when needed
BuildingBlocks         -> other BuildingBlocks projects, one-way, no cycles
```

The highest-value invariant is:

> `Application` must never reference `Infrastructure`.

Why: application handlers should remain testable without a database or infrastructure runtime, and persistence/integration implementations should remain replaceable behind application-facing abstractions.

Architecture tests should protect dependency direction so violations fail mechanically before code review. Do not rely on documentation alone when a rule can be encoded.

Identity's Worker is a class library of hosted background services. Its API
composition root registers the library with `AddIdentityWorker()`, giving the
dependency direction `API -> Worker -> Application`. Worker uses Application
abstractions and SharedKernel; Infrastructure supplies persistence implementations
through the API's DI container. Worker does not reference Infrastructure or API
and is not an independently executable or deployable host.

The executable `BookingSystem.ApiGateway` under `src/ApiGateway/` references shared HTTP primitives, response types and ServiceDefaults only. It owns allowlisted routing and edge policies, with no module project references, database access or business logic. Modules continue to validate bearer tokens and enforce their own permissions. See [gateway operations](docs/development/gateway.md).

Order owns per-showtime allocation, temporary reservations, orders and tickets in
its SQL Server database. Payment owns provider attempts, verified events, refunds
and reconciliation in its separate SQL Server database. Cinema remains PostgreSQL
and publishes versioned showtime/layout/price snapshots. Cross-module identifiers
are scalar references; local fulfillment never spans multiple databases.

Shared outbox/inbox/idempotency persistence belongs to BuildingBlocks.EntityFrameworkCore
and each module's local reliability schema. Redis is a future five-second availability
cache; database allocation remains authoritative. These persistence models do not
implement publishers, consumers, provider calls or workers. See
[booking reliability](docs/development/booking-reliability.md) for state/transaction
contracts and the explicitly deferred runtime work.

## 3. API boundary

Controllers are transport adapters. They should translate HTTP input into application commands/queries and send them through `ISender`.

Controllers must not inject:

- `DbContext`;
- `UserManager` / `RoleManager`;
- repositories;
- domain/business services directly.

If an endpoint appears to require those dependencies, first look for a handler-shaped solution consistent with the surrounding module.

## 4. CQRS

This repository uses the mediator in `BookingSystem.SharedKernel`; it does not use MediatR.

Application requests use abstractions such as:

- `ICommand`;
- `ICommand<TResponse>`;
- `IQuery<TResponse>`;
- `ICommandHandler<...>`;
- `IQueryHandler<...>`;
- `IValidator<TRequest>`.

Do not introduce `MediatR`, `IRequest`, or `IRequestHandler` unless an explicit architecture decision replaces the current mediator across the relevant scope.

## 5. Request pipeline

Do not reorder the request pipeline without an explicit architectural reason.

```text
Controller -> ISender.SendAsync(command/query)
  -> ValidationPipelineBehavior
  -> LoggingBehavior
  -> PerformancePipelineBehavior
  -> TracingPipelineBehavior
  -> TransactionPipelineBehavior   (commands only, via IUnitOfWork)
  -> CommandHandler / QueryHandler
  -> Result / Result<T>
```

The ordering expresses policy:

- validation rejects invalid requests before business work;
- logging/performance/tracing provide consistent cross-cutting visibility;
- commands receive transaction handling through the unit-of-work pipeline;
- handlers focus on use-case behavior rather than transport or transaction orchestration.

## 6. Persistence ownership

Commands run inside the transaction/unit-of-work pipeline and commit once after the handler completes successfully.

When the pipeline owns persistence completion:

- handlers must not call `SaveChangesAsync`;
- repositories must not call `SaveChangesAsync`;
- a use case should not create ad-hoc nested persistence completion paths.

Queries do not open command transactions.

If a future use case genuinely requires a different transaction model, record the architectural reason rather than silently bypassing this invariant.

## 7. Exception boundary

Unhandled `DomainException`-derived exceptions are translated into the standard `ApiResponse` error envelope by `GlobalExceptionHandlingMiddleware`.

Controllers should allow these exceptions to reach the global boundary rather than wrapping normal application flow in broad `try/catch` blocks.

## 8. Runtime architecture

Local orchestration, migration ownership, ServiceDefaults, and observability boundaries are documented in [docs/architecture/runtime-orchestration.md](docs/architecture/runtime-orchestration.md).

## 9. Architecture changes

For a change that alters a dependency direction, cross-module contract, transaction model, mediator model, database ownership, or another durable system constraint:

1. inspect the current implementation and relevant tests;
2. create or update an execution plan when the change is substantial;
3. record the durable decision under `docs/decisions/` when future agents need to understand why the architecture differs from the obvious alternative;
4. add or update mechanical enforcement where practical;
5. update this document if the repository-level model changed.
