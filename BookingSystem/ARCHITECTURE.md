# BookingSystem Architecture

This document is the repository-level architecture map. It explains the invariants that root `AGENTS.md` keeps concise and points to more specialized documentation where necessary.

## 1. System shape

BookingSystem is a modular monolith built with ASP.NET Core and Clean Architecture.

Business capabilities live under `src/Modules/<ModuleName>`. A module may contain `Api`, `Application`, `Domain`, `Infrastructure`, `Worker`, and `MigrationRunner` projects as required by that module.

Cross-module contracts live under `src/Contracts/`. Shared technical primitives live under `src/BuildingBlocks/`.

Do not infer that every reference/example module exists. Verify source before copying a pattern.

## 2. Dependency law

Allowed repository-level dependency direction:

```text
Module.Api             -> Module.Application + Module.Infrastructure + Contracts + BuildingBlocks
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
