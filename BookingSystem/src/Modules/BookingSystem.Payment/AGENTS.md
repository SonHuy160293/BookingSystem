# AGENTS.md — Payment

Root `AGENTS.md` and `src/Modules/AGENTS.md` continue to apply.

The Payment module owns payment capabilities. Keep its business rules and technical implementations inside this bounded context.

## Projects

| Project | Responsibility |
| --- | --- |
| `BookingSystem.Payment.API` | HTTP endpoints, middleware, Swagger, health checks, composition root |
| `BookingSystem.Payment.Application` | CQRS commands, queries, handlers, validators, and application abstractions |
| `BookingSystem.Payment.Domain` | Entities, domain rules, and domain events |
| `BookingSystem.Payment.Infrastructure` | EF Core, SQL Server, repositories, and external services |
| `BookingSystem.Payment.MigrationRunner` | Applies this module's EF Core migrations |

Application must not reference Infrastructure. Controllers communicate with Application through `ISender`. Infrastructure owns persistence, and handlers or repositories must not call `SaveChangesAsync` when the transaction pipeline owns the commit.

Do not add a Worker unless background processing, scheduled jobs, or queue consumption is required. Follow the repository-wide CQRS, domain, persistence, API, and verification rules when adding future behavior.

## Payment persistence ownership

Payment owns attempts, verified normalized provider events, full refunds and
reconciliation. Unknown provider outcomes remain active and block another charge.
Persist provider idempotency keys before external calls. Never treat browser
redirects as financial evidence or hold a transaction across a provider call.

Verified late/additional charges must be retained and refunded/reviewed. Outbox,
inbox and idempotency tables are shared technical persistence, local to this
database. Domain methods require complete aggregate collections. See
[booking reliability](../../../docs/development/booking-reliability.md).
