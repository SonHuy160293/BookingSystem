# AGENTS.md — Identity

Additional instructions for Codex working under `src/Modules/BookingSystem.Identity/`.

Root `AGENTS.md` and `src/Modules/AGENTS.md` rules continue to apply.

The Identity module owns accounts, roles, permissions, authentication/tokens, and avatar storage.

## Projects

| Project                                  | Responsibility                                                       |
| ---------------------------------------- | -------------------------------------------------------------------- |
| `BookingSystem.Identity.Api`             | HTTP endpoints, middleware, Swagger, health checks, composition root |
| `BookingSystem.Identity.Application`     | CQRS use cases: commands, queries, handlers, validators              |
| `BookingSystem.Identity.Domain`          | Entities, domain rules, domain events                                |
| `BookingSystem.Identity.Infrastructure`  | EF Core, SQL Server, repositories, external services                 |
| `BookingSystem.Identity.MigrationRunner` | Applies EF Core migrations                                           |
| `BookingSystem.Identity.Worker`          | Background services class library, hosted by Identity API             |

Keep Identity-specific code inside this module rather than moving it into `BuildingBlocks` unless it is genuinely reusable technical infrastructure.

Tenant owns chain branding through its existing Name and optional Slug and LogoUrl properties. Non-null slugs are globally unique and normalized to lowercase. Cinema.Id reuses Branch.Id for its venue profile, while Cinema.TenantId remains a scalar tenant reference. There are no cross-module database foreign keys or entity navigations. Branch name and address serve administrative workflows; Cinema owns public venue presentation. Existing Identity Branch references remain unchanged.

## Application

CinemaAccess integration queries expose current role-derived scopes and verified Branch ownership to trusted services through IntegrationApiController's service-key filter. They do not expose domain entities or grant access from a raw permission claim. See [Cinema management API](../../../docs/development/cinema-management-api.md) for the wire contract and configuration.

Organize use cases by feature:

```text
Features/<Area>/
├── Commands/<Action><Area>/
│   ├── <Action><Area>Command.cs
│   ├── <Action><Area>CommandHandler.cs
│   └── <Action><Area>CommandValidator.cs
└── Queries/<GetSomething>/
    ├── <GetSomething>Query.cs
    └── <GetSomething>QueryHandler.cs
```

Rules:

* Use queries for reads and commands for state changes.
* Use events only when another workflow must react to completed behavior.
* Handlers and validators are discovered by assembly scanning; do not manually register them.
* Query handlers return DTOs/read models, not EF entities.
* Keep EF queries in Infrastructure repositories/read services.
* Never call `SaveChangesAsync` from handlers or repositories. The command transaction pipeline owns the commit.
* Application may depend on abstractions such as `IUnitOfWork`, but never on `ApplicationDbContext` or Infrastructure implementations.
* For pagination, prefer module-specific request types inheriting `PagedRequest` rather than exposing `PagedRequest` directly.

## Domain

Keep Domain focused on business state, rules, and events.

Domain may use domain-safe primitives from `SharedKernel`, but must not depend on CQRS, persistence, HTTP, or infrastructure concerns.

No:

* EF Core types;
* Infrastructure references;
* `ICommand` / `IQuery`;
* handlers or validators;
* `IUnitOfWork`;
* mediator or pipeline behaviors.

Use the existing entity contracts and base classes where appropriate:

* `IEntity<TId>`;
* `IAuditableEntity`;
* `ISoftDeletableEntity`;
* `Entity<TId>`;
* `SoftDeletableEntity<TId>`.

## Infrastructure

Infrastructure owns persistence and external technical integrations.

`RepositoryBase<TEntity>` currently uses:

```text
EfRepositoryBase<TEntity, Guid, ApplicationDbContext>
```

Keep this wrapper for aggregates using `Guid`.

If a future aggregate requires another ID type, add an appropriate wrapper rather than changing the existing one for unrelated aggregates.

Repositories may add, update, delete, and query entities, but must not call `SaveChangesAsync`.

Audit fields are populated through `AuditSaveChangesInterceptor` and `ICurrentUserProvider`.

Do not manually populate `CreatedBy` or `UpdatedBy` in handlers.

Only add an EF Core migration when the data model changes.

Migrations must target this module's `ApplicationDbContext`.

Do not hand-edit generated migrations unless there is a specific, understood reason to do so.

## Api

Controllers communicate with Application through `ISender`.

Do not inject into controllers:

* `DbContext`;
* repositories;
* EF Core services;
* domain services;
* Infrastructure implementations.

Controllers should only:

```text
HTTP input
    ↓
command/query
    ↓
ISender
    ↓
Result
    ↓
HTTP response
```

Use the appropriate controller base:

* `ApiController` — normal authenticated APIs;
* `PublicApiController` — public APIs;
* `IntegrationApiController` — server-to-server/integration APIs.

Infrastructure references in Api should be limited to composition-root and operational concerns such as dependency registration, migrations/seeding, and health checks.

Let known application/domain exceptions bubble to the global exception middleware.

Do not catch and re-wrap them in controllers.

## Worker and MigrationRunner

`BookingSystem.Identity.Worker` is a class library containing hosted background
services. The API references it and calls `AddIdentityWorker()` after registering
Application and Infrastructure. It has no executable entry point or separate
deployment. See [Worker guidance](BookingSystem.Identity.Worker/AGENTS.md) and
[Worker README](BookingSystem.Identity.Worker/README.md).

Worker references Application and SharedKernel, never Infrastructure or API.
Keep scheduling and scoped orchestration in Worker; business rules, repositories,
and command transactions remain in their existing layers.

`MigrationRunner` is a separate thin host applying pending migrations for
`BookingSystemIdentityDbContext`. It does not register the Worker services.

Do not place business logic in Worker or MigrationRunner. Invoke Application
commands/queries or appropriate application abstractions instead.

## Adding a CQRS feature

For a write use case, typically consider:

```text
request/response contract
→ permission, if required
→ command
→ validator
→ handler
→ repository/service changes, if required
→ controller action
→ event, only when another workflow must react
→ tests
```

For a read use case:

```text
response contract
→ query
→ handler
→ repository/read service
→ controller action
→ tests
```

If `.codex/skills/add-cqrs-feature/` exists, use that Codex skill for the workflow.

Verification follows the mode requested by the user's prompt and the root `AGENTS.md`.

Do not automatically run `./scripts/verify.sh` merely because a CQRS feature was added.
