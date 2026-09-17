# AGENTS.md — Cinema

Root `AGENTS.md` and `src/Modules/AGENTS.md` continue to apply.

The Cinema module owns cinema capabilities. Keep its business rules and technical implementations inside this bounded context.

## Projects

| Project | Responsibility |
| --- | --- |
| `BookingSystem.Cinema.API` | HTTP endpoints, middleware, Swagger, health checks, composition root |
| `BookingSystem.Cinema.Application` | CQRS commands, queries, handlers, validators, and application abstractions |
| `BookingSystem.Cinema.Domain` | Entities, domain rules, and domain events |
| `BookingSystem.Cinema.Infrastructure` | EF Core, PostgreSQL, repositories, and external services |
| `BookingSystem.Cinema.MigrationRunner` | Applies this module's EF Core migrations |

Application must not reference Infrastructure. Controllers communicate with Application through `ISender`. Infrastructure owns persistence, and handlers or repositories must not call `SaveChangesAsync` when the transaction pipeline owns the commit.

Cinema uses PostgreSQL through Npgsql. Configure `ConnectionStrings:ApplicationDb` with PostgreSQL syntax and retries through `PostgreSql`. Use PostgreSQL-compatible migrations and SQL. Audit timestamps use `timestamp with time zone` and UTC `DateTimeOffset` values. MigrationRunner owns migration execution; API startup must not apply migrations.

Do not add a Worker unless background processing, scheduled jobs, or queue consumption is required. Follow the repository-wide CQRS, domain, persistence, API, and verification rules when adding future behavior.
