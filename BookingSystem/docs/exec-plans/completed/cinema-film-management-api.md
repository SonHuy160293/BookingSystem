# Cinema and Film management APIs

## Scope and decisions
Implement four versioned management endpoints each for Cinema and Film using add-cqrs-api-feature. Film access requires a platform permission. Cinema access respects tenant/branch role assignments. Cinema.Id remains Identity Branch.Id and TenantId comes from verified Branch ownership.

Use EF repositories for reads and writes, custom CQRS, existing transaction/audit pipeline, and no schema migrations. Identity exposes service-key-protected access/Branch lookups; Cinema calls through an Application abstraction and HTTP Infrastructure adapter. No cross-module entity/project references. Token issuance remains outside scope.

## Milestones
- [x] Identity integration and Cinema authorization.
- [x] Contracts, validators, handlers, repositories, controllers.
- [x] DI, commit conflict translation, operational guidance.
- [x] Targeted builds and behavioral/SQL/routing inspection.

## Verification
Verify scoped counts and resources, immutable ownership, Film genre reconciliation, JSONB projection, deterministic paging, validation and known constraint conflicts. Build affected APIs with analyzers; use existing architecture checks. No new permanent test infrastructure. Runtime database verification depends on available local services.

## Rollout
Requires configured JWT validation, service integration credentials, existing Cinema schema/reference data, and synchronized permission definitions/assignments. Do not apply migrations or modify existing data during implementation.

See [Cinema management API](../../development/cinema-management-api.md) for routes, request semantics, authorization and configuration.

## Verification results

- Both API builds passed with zero errors. The final Cinema incremental build had zero warnings; Identity retains existing analyzer warnings. Clean compilation also reports existing shared ApiController comment warnings.
- Existing architecture tests passed: 3/3.
- Temporary inspection used EF InMemory, the actual handlers/repositories/validators, a local Kestrel host with JWT authentication, and command interceptors for provider SQL translation. All eight HTTP endpoints passed, including create Location links, committed audit timestamps, required input, 401/403/404/409 responses and sorting validation.
- Confirmed scoped counts, tenant isolation, branch-only access, wrong tenant/Branch pair rejection, immutable Cinema ownership, Film platform authorization, disabled-permission revocation, genre replacement and lookup integrity, empty list persistence and extreme paging.
- Confirmed PostgreSQL JSONB/genre detail projection, search/genre filtering and scoped-count translation; SQL Server access-join translation; HTTP integration envelope parsing and fail-closed upstream failure.
- Injected provider failures confirmed known Cinema uniqueness conflicts and deleted lookup handling at the unit-of-work boundary; unknown database errors remain unchanged.
- Production DI registrations resolved. Integration registration is API-only so MigrationRunner does not acquire a credential dependency.
- Docker daemon was unavailable. No live SQL Server/PostgreSQL constraint or transaction execution was performed; InMemory checks are not evidence of database constraint execution.
- No migrations, business data changes or permanent test infrastructure were added.

## Necessary compatibility fixes

The existing Identity build failed before the new endpoint could compile: CreateUser omitted the newly required BranchId argument, and AddUserRole called a scoped HasRoleAsync overload absent from the repository interface/implementation. CreateUser now explicitly supplies null for its existing request contract; role duplicate detection now includes tenant/branch scope. No new user API fields were introduced.

## Implementation notes

CodeGraph and current source were used for reference investigation. Identity Branch endpoints supplied the controller/CQRS/EF pattern; their absence of scoped authorization was not copied. No Dapper read repository was introduced. Static DTO mapping and existing DbContext registration/audit conventions were reused.

The Identity integration uses the existing service-key filter instead of user PermissionAuthorize attributes because it serves authenticated service calls. Public management actions use both permission filters and scoped Application authorization. Token issuance remains a prerequisite outside this implementation.
