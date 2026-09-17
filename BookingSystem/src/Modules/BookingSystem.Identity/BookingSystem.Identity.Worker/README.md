# BookingSystem.Identity.Worker

A .NET 10 class library containing Identity background services. The Identity API
hosts these services in its own process. This project has no executable entry
point, independent configuration file, container, or AppHost resource.

## Registration and dependencies

The API references this project and registers it after Application and
Infrastructure:

```csharp
using BookingSystem.Identity.Worker.DependencyInjection;

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddIdentityWorker();
```

`AddIdentityWorker()` registers one `AuthSessionCleanupService` as an
`IHostedService`; repeating registration does not duplicate the service.
The API supplies validated AuthSessionOptions, TimeProvider, logging, scoped
repositories, and ISender.

Dependency direction is `API -> Worker -> Application`, with Worker also
referencing SharedKernel and the centrally managed hosting/DI abstractions.
Worker does not reference Infrastructure or API. Persistence implementations are
provided through the host's DI container, rather than referenced by this library.
MigrationRunner does not register this background service.

## Authentication session cleanup

`Services/AuthSessionCleanupService.cs` starts a PeriodicTimer when hosted and
waits 24 hours before its first pass. Subsequent passes wait for the daily timer.
API restarts reset that timer; there is no persisted schedule or catch-up run.
The service stops with the API process.

Each pass discovers eligible user IDs through the Application repository
abstraction, then sends CleanupSessionsCommand through ISender in a fresh
asynchronous scope for each user. The pass performs at most 100 discovery rounds.
Singleton hosted services never retain scoped repositories or senders.

Application and Infrastructure own eligibility, per-user SQL locking, EF
transactions, and deletion. The default retention is seven days beyond the
session's fixed 30-day absolute expiry. Each transaction processes at most 100
session families and 1,000 token records. Large terminal families drain over
multiple batches; active/recent families retain replay-detection history.

Cancellation propagates to waits and all database/command operations. Cleanup
failures log their exception type without potentially sensitive exception text
and are retried on a later daily pass, rather than terminating normal scheduling.
The logger category follows the service's new Worker namespace.

Multiple API replicas may host cleanup simultaneously. Existing SQL Server
user-row locks and command transactions coordinate deletion across processes;
the service does not rely on in-memory locks or leader election. The class-library
extraction does not change retention, session revocation, or database behavior.

## Configuration

Settings remain in the API's `AuthSessions` section:

| Setting | Default | Meaning |
| --- | --- | --- |
| CleanupEnabled | true | Exit without starting the timer when false |
| RetentionDays | 7 | Retain token history this many days beyond absolute expiry |
| CleanupBatchSize | 100 | Maximum families per user transaction; validated between 1 and 100 |

`AbsoluteExpirationDays` remains 30 by default and is fixed for each session at
login. Cleanup uses each stored AbsoluteExpiresAt value. No Worker-specific copy
of these settings is required.

For the HTTP contract and authentication policies, see
[Identity authentication sessions](../../../../docs/development/identity-auth-sessions.md).
Engineering boundaries are documented in [AGENTS.md](AGENTS.md).

## Verification

From the repository root:

```powershell
dotnet build src/Modules/BookingSystem.Identity/BookingSystem.Identity.Worker
dotnet build src/Modules/BookingSystem.Identity/BookingSystem.Identity.API
dotnet test tests/BookingSystem.Identity.Tests
dotnet test tests/BookingSystem.ArchitectureTests
```

Identity SQL/API tests require `BOOKING_IDENTITY_TEST_SQL` pointing to SQL Server
with permission to create a temporary database. The existing fixture verifies
database isolation before seeding and removes only its own test database.
