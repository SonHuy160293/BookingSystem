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

## Catalog and venue ownership

Region, Genre, and Film are shared catalog data. Region and Genre have locally assigned integer keys without audit inheritance; FilmGenre is an unaudited association. Film and Cinema reuse the shared audited entity base.

Cinema owns venue presentation data. Identity's Tenant owns chain name, slug, and logo alongside its administrative information. Cinema.Id reuses Identity Branch.Id and must be supplied to its factory; Cinema does not generate a separate venue identity. The primary key permits at most one Cinema profile per Branch. TenantId remains an external scalar identifier with no Identity database foreign key or entity navigation. Cinema owns public venue name and address; Identity Branch retains administrative name and address. The create workflow verifies Branch ownership through the application integration boundary before persisting the profile.

Catalog tables use the `cinema` schema with plural PascalCase table names and PascalCase column names. Quote PascalCase table and column identifiers in custom PostgreSQL SQL. Film genres are relational; country codes, descriptive format tags, and caption options use native primitive-collection JSONB mappings. These metadata lists do not define bookable screening combinations.

## Management APIs

Cinema/Film paging, detail, create and update use EF repositories and the custom CQRS pipeline. Film permissions require platform scope. Cinema queries and commands enforce current tenant/branch grants through the Identity integration boundary; permission claims alone are insufficient. Creation derives TenantId from the verified Branch, not public input. Apply scope before counts and pagination.

Film updates must track memberships and mark only the aggregate modified for auditing; never mark lookup Genres modified through a broad graph update. Write controllers read the committed result using the same action permission so audit timestamps are current.

See [Cinema management API](../../../docs/development/cinema-management-api.md) for routes, configuration, integration contracts and rollout requirements. Keep Identity HTTP client registration API-specific; MigrationRunner must not require integration credentials.

Do not add a Worker unless background processing, scheduled jobs, or queue consumption is required. Follow the repository-wide CQRS, domain, persistence, API, and verification rules when adding future behavior.

## Booking publication model

Showtime has an application-managed positive Revision and immutable first
cancellation metadata. Cancellation is permitted after sales close. Published
layout/price snapshots remain immutable; Order owns per-showtime booking allocation
in its own SQL Server database. Shared reliability records are local technical
persistence; event contracts and publication are separate runtime work.
See [booking reliability](../../../docs/development/booking-reliability.md).
