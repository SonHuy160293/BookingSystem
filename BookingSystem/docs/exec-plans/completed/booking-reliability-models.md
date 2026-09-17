# Booking reliability persistence models

## Approved outcome

Implement the accepted create-ef-entity plan for Cinema, Order and Payment:
domain models and guards, Fluent mappings, existing DbContext registration,
shared technical persistence records and durable architecture documentation.

Cinema remains PostgreSQL. Order owns seat inventory, reservations, orders and
tickets in SQL Server. Payment owns provider attempts, events, full refunds and
reconciliation in SQL Server. Shared outbox/inbox/idempotency records belong to
BuildingBlocks.EntityFrameworkCore and each module's own database.

Guest checkout requires hashed access credentials and an order email. Holds expire
after five minutes or showtime start, whichever comes first. Positive order totals
are required. Late/unfulfillable payments receive full refunds. No partial refunds,
paid-seat resale, ticket reissuance or cross-database transaction.

## Milestones

- [x] Shared technical records and provider-aware mappings.
- [x] Cinema revision and cancellation metadata.
- [x] Order domain models, constraints and registration.
- [x] Payment domain models, constraints and registration.
- [x] Architecture/runtime contracts and source/diff review.

## Explicit execution constraints

No seed data, migrations, APIs, repositories, application handlers, workers,
provider calls, RabbitMQ transport, Redis operations or tests. No restore, build,
test, formatter, Docker execution, migration execution or verification loop.
Source/diff review only; do not claim executable concurrency validation.

## Runtime follow-up

Atomic allocation/confirmation/expiration, verified callbacks, reconciliation,
outbox publishers/inbox consumers, notification retry delivery and a five-second
Redis availability cache require separate implementations and executable checks.
Schema deployment remains owned by module MigrationRunners in a later task.

## Progress

- Inspected current module rules, empty Order/Payment contexts, shared EF project,
  existing Cinema seating and Showtime lifecycle.
- Added reusable outbox/inbox/idempotency records, lease fencing, concurrency tokens
  and provider-aware reliability mappings; pinned Relational at existing EF 10.0.9.
- Added Cinema revision/cancellation metadata; eight Order and five Payment business
  entities, domain guards, explicit constraints/indexes and three context integrations.
- Added aggregate collection counts to detect incomplete loads before critical
  mutations and kept unresolved provider mismatches behind an explicit review decision.
- Documented booking-reliability.md, module ownership, architecture and runtime
  boundaries. Finished source/diff review only; no executable checks were performed.
- Remaining work is explicitly outside this change: migrations/deployment, runtime
  transactions, APIs, provider adapters, workers, messaging and Redis integration.
