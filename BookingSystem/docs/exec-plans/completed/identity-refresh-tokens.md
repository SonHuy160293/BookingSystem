# Rotating refresh tokens and device sessions

## Outcome and decisions

Implement the accepted refresh-token plan in Identity: opaque 256-bit tokens,
SHA-256-only storage, strict rotation/reuse revocation, independent login sessions,
five-minute JWTs, seven-day idle and 30-day absolute session limits.
JSON transport targets native/server clients. JWT revocation takes effect at expiry.

Use SQL Server user-row update locks to serialize authentication mutations across
replicas. Authentication transactions never automatically retry, including after
an uncertain commit. Replay revocation commits before returning an auth failure.
Ordinary command transactions retain their existing retry policy.

## Implementation slices

- [x] Domain entities, EF mappings/repositories, cryptographic token service.
- [x] Login, refresh, logout, logout-all, session list and individual revocation.
- [x] Authentication transaction policy, HTTP authentication, rate limits, cleanup.
- [x] Additive generated migration and rollout documentation.
- [x] Unit, real SQL Server concurrency/API tests, architecture tests, solution build.

## Verification and rollout

Use isolated SQL Server databases and independent connections for concurrency
tests; never reset the developer's IdentityDb. Apply the generated migration using
the existing MigrationRunner. Deploy schema before all Identity replicas and then
clients. Existing JWTs retain their original expiry; rollback keeps session tables.
Preserve pre-existing working-tree changes and migrations.

## Progress

- Repository and existing auth flow inspected; SQL Server container is available.
- Enabled API authentication and endpoint rate limits. Preserved auth 204 responses
  with a local envelope wrapper; other modules retain their response behavior.
- Added UTC datetime2 mappings, rowversion guards, parameterized SQL user locks,
  and authentication transactions with zero automatic retries.
- Cleanup is API-hosted and bounds both families (100) and token rows (1,000).
- Generated migration `20261014093033_AddAuthSessionsAndRefreshTokens` and applied
  it through Identity MigrationRunner to local IdentityDb. EF reports no pending
  model changes; the migration adds only AuthSessions and RefreshTokens.
- Corrected an initial test-host connection override mistake. Removed precisely
  the 17 temporary users it had seeded in IdentityDb. Added a database-name
  assertion before test seeding; temporary test databases are removed on teardown.
- Verification: 33 Identity tests passed, including real SQL Server concurrency,
  rollback/uncertain-commit, replay, ownership, expiry, cleanup and API tests;
  3 architecture tests and 3 SharedKernel tests passed; full solution build passed.
- Existing analyzer/package warnings remain. Repository-wide diff whitespace
  checking flags pre-existing whitespace in RoleAssignmentRequest, outside this work.
- Durable API/client/transaction/rollout guidance lives in
  [Identity authentication sessions](../../development/identity-auth-sessions.md).
