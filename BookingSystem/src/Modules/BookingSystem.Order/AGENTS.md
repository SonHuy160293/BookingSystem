# AGENTS.md — Order

Root `AGENTS.md` and `src/Modules/AGENTS.md` continue to apply.

The Order module owns order capabilities. Keep its business rules and technical implementations inside this bounded context.

## Projects

| Project | Responsibility |
| --- | --- |
| `BookingSystem.Order.API` | HTTP endpoints, middleware, Swagger, health checks, composition root |
| `BookingSystem.Order.Application` | CQRS commands, queries, handlers, validators, and application abstractions |
| `BookingSystem.Order.Domain` | Entities, domain rules, and domain events |
| `BookingSystem.Order.Infrastructure` | EF Core, SQL Server, repositories, and external services |
| `BookingSystem.Order.MigrationRunner` | Applies this module's EF Core migrations |

Application must not reference Infrastructure. Controllers communicate with Application through `ISender`. Infrastructure owns persistence, and handlers or repositories must not call `SaveChangesAsync` when the transaction pipeline owns the commit.

Do not add a Worker unless background processing, scheduled jobs, or queue consumption is required. Follow the repository-wide CQRS, domain, persistence, API, and verification rules when adding future behavior.

## Booking persistence ownership

Order owns ShowtimeInventory/ShowtimeSeat, reservations, orders, tickets and
refund requests. External Cinema/Identity/Payment IDs are scalar snapshots, never
cross-module database foreign keys. Keep current allocation separate from historical
reserved-seat records. Concurrency correctness requires a later local SQL Server
transaction spanning guarded allocation, confirmation, tickets and inbox/outbox.

Guest ownership stores only a secret hash; ownership authorization belongs at the
application boundary. Domain methods require complete aggregate collections.
See [booking reliability](../../../docs/development/booking-reliability.md).
