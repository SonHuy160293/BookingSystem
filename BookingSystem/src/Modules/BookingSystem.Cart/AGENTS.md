# AGENTS.md — Cart

Root `AGENTS.md` and `src/Modules/AGENTS.md` continue to apply.

The Cart module owns cart capabilities. Keep its business rules and technical implementations inside this bounded context.

## Projects

| Project | Responsibility |
| --- | --- |
| `BookingSystem.Cart.API` | HTTP endpoints, middleware, Swagger, health checks, composition root |
| `BookingSystem.Cart.Application` | CQRS commands, queries, handlers, validators, and application abstractions |
| `BookingSystem.Cart.Domain` | Entities, domain rules, and domain events |
| `BookingSystem.Cart.Infrastructure` | EF Core, SQL Server, repositories, and external services |
| `BookingSystem.Cart.MigrationRunner` | Applies this module's EF Core migrations |

Application must not reference Infrastructure. Controllers communicate with Application through `ISender`. Infrastructure owns persistence, and handlers or repositories must not call `SaveChangesAsync` when the transaction pipeline owns the commit.

Do not add a Worker unless background processing, scheduled jobs, or queue consumption is required. Follow the repository-wide CQRS, domain, persistence, API, and verification rules when adding future behavior.
