# Paged Permissions API using EF Core

Status: Completed (source implementation; executable verification omitted by request)
Date: 2026-10-10

## Implementation

Applied [add-cqrs-api-feature](../../../.codex/skills/add-cqrs-api-feature/SKILL.md), following the existing EF-based Tenant paging feature. `GET /api/v1/permissions` binds GetPermissionsRequest and returns PagedResult of PermissionDto containing exactly Name, Value, GroupName, Description, and ScopeType. The controller uses ISender and declares 200 and 400 responses without authorization attributes.

ScopeType is optional. Null or trimmed blank input adds no filter; supplied values are trimmed and uppercased, and only PLATFORM, TENANT, and BRANCH are accepted by the query validator. Pagination retains existing normalization.

IPermissionRepository uses the shared generic repository contract. PermissionDefinition implements IEntity of Guid through its existing Id without audit inheritance or schema changes. PermissionRepository is registered in Infrastructure DI and uses the existing no-tracking Query method. Filtering applies enabled status, exact normalized scope, and search across Name, Value, GroupName, and nullable Description before count/pagination. Rows are projected directly to PermissionDto.

Sort fields are Name, Value, GroupName, and ScopeType in either direction; missing/unsupported fields use DisplayOrder ascending. Every sort has an Id tie-breaker. Page normalization defaults to page 1 and size 20, caps size at 100, and protects large offsets from overflow. Empty/out-of-range pages retain the filtered total count.

## Acceptance scenarios (documented, not executed)

- Response items contain the five requested fields and exclude disabled permissions.
- Each valid scope returns only matching enabled rows; omitted/blank scope returns all enabled scopes.
- Lowercase and padded scope input is normalized; other supplied values produce HTTP 400.
- Search combines with scope filtering and handles null Description.
- Each supported sort respects direction and deterministic Id tie-breaking; default/unsupported sorting uses DisplayOrder ascending.
- Invalid page inputs normalize, large page size caps at 100, and empty/out-of-range/very large pages retain correct totals without offset overflow.

## Boundaries and review

Only source/diff review was performed. No tests, builds, verification scripts, migrations, Dapper repository, authorization changes, catalog changes, or seeder changes were made. Existing PermissionDefinition persistence names, RBAC, Tenant APIs, UserClaim, and unrelated working-tree edits are preserved. No executable verification result is claimed.
