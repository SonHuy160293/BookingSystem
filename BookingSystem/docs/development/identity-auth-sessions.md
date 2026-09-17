# Identity authentication sessions

Identity issues five-minute JWT access tokens and rotating opaque refresh tokens.
The default session expires after seven days without a successful refresh and has
a fixed 30-day maximum lifetime. Native clients store refresh tokens in OS secure
storage; server clients use a protected secret store. JSON transport is not a
durable browser-token storage design.

## HTTP contract

Responses retain the existing API envelope: successful token responses contain
`success: true`, authentication fields under `data`, and `error: null`.
`data` contains `accessToken`, `user`, `refreshToken`, `sessionId`,
`accessTokenExpiresAt`, and `refreshTokenExpiresAt`. Expiration timestamps are UTC.
Token responses have `Cache-Control: no-store` and `Pragma: no-cache`.

| Method and route | Input | Success |
| --- | --- | --- |
| POST /api/Auth/login | Email, Password, optional ClientId and DeviceName | 200, token pair and user |
| POST /api/Auth/refresh | RefreshToken, ClientId | 200, replacement token pair and current user |
| POST /api/Auth/logout | RefreshToken, ClientId | 204 |
| POST /api/Auth/logout-all | Bearer access token | 204 |
| GET /api/Auth/sessions | Bearer access token | 200, active sessions |
| DELETE /api/Auth/sessions/{sessionId} | Bearer access token, owned session ID | 204 |

Login defaults an omitted client ID to `booking-system`. Refresh and logout
require the client ID. The allowlist controls which application identifiers can
receive tokens; a client ID is not a secret or proof of device possession.
Each login creates an independent session, even when the device label is reused.

Session listing returns sessionId, clientId, deviceName, createdAt,
lastRefreshedAt, idleExpiresAt, absoluteExpiresAt, and isCurrent, ordered by last
refresh time. It exposes neither token hashes nor security stamps. Another user's
session ID returns 404 on revocation. Unknown or already-revoked logout tokens
and repeated revocation of an owned session return 204.

Malformed token/client input returns 400. Invalid refresh credentials return a
generic 401. Rate-limit rejection returns 429. Authentication database failures
return 503 without token material. Logout accepts token possession without a JWT,
including a consumed token from the same family, and works after JWT expiry.

## Rotation, theft, and concurrency

Refresh tokens contain 32 bytes from `RandomNumberGenerator.GetBytes(32)`, encoded
as unpadded canonical Base64Url. SQL Server stores only SHA-256 of the UTF-8 wire
value in binary(32), with a unique hash index. Tokens are bearer credentials: a
stolen current token can succeed before reuse is detected.

`AuthSessions` is the token family. Refresh atomically consumes its current token,
inserts a successor, advances the current-token pointer, and extends idle expiry
up to the fixed absolute limit. Refresh reloads current account, roles, and
permissions. Disabled, locked-out, two-factor-required, missing, or security-stamp-
changed accounts cannot refresh.

Reusing any consumed token while its family is active revokes the family,
including when the consumed token's own expiry has passed. Revocation commits
before the authentication failure is returned. Other families remain active.

**Clients must allow only one refresh request at a time per session.** Coordinate
all callers within an installation and atomically replace the stored refresh
token after success. Two requests using the same token can produce one 200 and
one 401; the losing request revokes the family, so the winner's successor also
becomes unusable. There is no duplicate grace window.

Clients and reverse proxies must not automatically retry refresh. After a lost
response or 503, clear local credentials and require login. The server cannot
recover a successor's plaintext from its hash.

Logout/replay revocation stops refresh immediately. JWTs remain valid until
expiry, so an already-issued access token can still work for up to five minutes.
Legacy JWTs keep their originally issued expiry, potentially 60 minutes.

## Persistence and transaction policy

EF Core uses the existing Identity DbContext and tracked repositories. Session
and token timestamps use datetime2(7) with UTC materialization converters; both
tables have SQL Server rowversion columns. The session's current-token pointer
has no reverse foreign key to avoid a cyclic insert dependency. Refresh validates
token/session ownership and pointer membership explicitly.

Every authentication mutation takes a parameterized `UPDLOCK, HOLDLOCK` read of
the owning Users row within its command transaction. Lock order is user first,
then session/token state. Token-owner discovery is untracked and is followed by
authoritative reads after locking. The user lock serializes operations across
replicas, including logout-all versus login: sessions created after logout-all's
serialized execution point remain valid.

An Identity-local transaction behavior routes auth commands through an outer SQL
execution strategy with zero retries. This also suppresses nested provider retries.
Ordinary commands retain their existing retry strategy. A connection failure
during commit has an uncertain outcome and must not replay rotation. Only the unit
of work calls SaveChanges and commits. Security logs identify attempted mutations
and separately record committed session changes; SQL failures log type names
without exception text or credential parameters.

Future password-change, account-disable/delete, and security-stamp workflows must
participate in this user-lock/revocation protocol. Do not add an alternate session
mutation path or process-local mutex.

## Configuration, maintenance, and rollout

`AuthSessions` configures DefaultClientId, AllowedClientIds, IdleExpirationDays,
AbsoluteExpirationDays, RetentionDays, CleanupBatchSize, and CleanupEnabled.
`Jwt:AccessTokenExpirationMinutes` must be between one and five; Identity validates
this at startup. Default retention is seven days beyond absolute expiry.

The API registers `BookingSystem.Identity.Worker` through `AddIdentityWorker()`
and hosts its `AuthSessionCleanupService` in the same process. The Worker class
library owns scheduling and invokes Application commands; persistence remains in
Infrastructure. See the [Worker README](../../src/Modules/BookingSystem.Identity/BookingSystem.Identity.Worker/README.md)
for registration, scheduling, and configuration.

A cleanup transaction removes
at most 100 session families and 1,000 token rows under the owning user's lock.
Large terminal families are drained over multiple batches. Active/recent families
retain all consumed hashes. Multiple replicas may run cleanup safely.

`RateLimiting:AuthPublic` defaults to five login attempts per IP per ten minutes.
`RateLimiting:AuthRefresh` defaults to 60 refresh/logout or management requests per
minute. Policies partition authenticated traffic by user and anonymous traffic by
IP. Counters are per process and budgets multiply with replicas; enforce a shared
ingress budget where required. Configure only trusted forwarded proxies.

All replicas must share the authoritative SQL Server primary, matching JWT
issuer/audience/signing configuration, synchronized clocks, and HTTPS. Inject
production signing secrets through deployment configuration. Do not use a lagging
read replica, sticky sessions, or an in-memory revocation cache as an auth authority.

Apply `AddAuthSessionsAndRefreshTokens` with Identity MigrationRunner before
deploying all Identity API replicas and then enabling the client refresh flow:

```powershell
dotnet run --project src/Modules/BookingSystem.Identity/BookingSystem.Identity.MigrationRunner
```

Supply `ConnectionStrings__ApplicationDb` for the intended environment and preserve
database volumes. The migration only adds authentication tables and indexes.
Users obtain refresh sessions at their next login. Rollback application code
without dropping session tables or retained token history.

## Tests

Run Identity tests with `dotnet test tests/BookingSystem.Identity.Tests`.
Unit tests always run. Set `BOOKING_IDENTITY_TEST_SQL` to a SQL Server connection
authorized to create a temporary database to run SQL/API tests. The fixture creates
and deletes only a UUID-named `BookingIdentityTests_*` database and asserts the API
connection targets it before seeding. No Testcontainers or EF InMemory substitute
is used for concurrency validation.
