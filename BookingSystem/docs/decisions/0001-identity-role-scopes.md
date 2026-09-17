# Identity role ownership and assignment scopes

Status: Accepted
Date: 2026-10-08

## Context

Identity must represent many tenants, each owning many branches, while allowing reusable role definitions and assigning the same role to one user in several tenants or branches. The previous `(UserId, RoleId)` assignment key and global role-name conflict check cannot represent that behavior.

## Decision

Role `TenantId` is definition ownership, while UserRole `TenantId` and `BranchId` identify where a grant applies. These concepts have different null semantics:

- A Role with null `TenantId` is reusable across the platform. `ScopeType` determines whether grants apply at PLATFORM, TENANT, or BRANCH scope. A TENANT or BRANCH definition may also be owned by one tenant; PLATFORM definitions have no tenant owner.
- A UserRole with neither tenant nor branch is a PLATFORM assignment. A TENANT assignment requires only a tenant; a BRANCH assignment requires both. Tenant-owned definitions cannot be assigned outside their owner tenant.
- Branch ownership is required. A branch grant must refer to a branch in the grant's tenant.
- UserRole has a UUID v7 primary key without inherited audit columns, with exact uniqueness across user, role, tenant, and branch, including null scope IDs.
- Normalized role names are unique within their owner and scope. Null ownership is the reusable-definition namespace; null normalized names are excluded.
- Role and PermissionDefinition scope values are stored as `varchar(8)` strings `PLATFORM`, `TENANT`, or `BRANCH`. Existing factory calls default to PLATFORM; permission synchronization without an explicit scope preserves existing metadata.

## Why

Separating ownership from grant scope lets one reusable definition serve many tenants without turning a tenant or branch grant into a platform grant. A surrogate assignment key supports nullable scope dimensions and repeated grants; database uniqueness still prevents duplicate assignments.

## Consequences and enforcement

Domain factories validate scope shape and ownership. CQRS callers validate input and loaded role/branch compatibility. EF mappings provide uniqueness constraints, allowed-value checks, tenant foreign keys, and a composite branch/tenant foreign key. New tenant/branch relationships use `NoAction` deletion.

This change prepares the model only. It does not implement runtime tenant authorization, claim generation, tenant isolation, or permission-catalog scope classification. UserClaim remains unchanged. A future authorization implementation must interpret the grant scope explicitly rather than treating all stored grants as platform permissions.

The user excluded migrations, tests, builds, and executable verification. Before database rollout, a separate migration must create tenants, backfill existing branch ownership, populate assignment IDs and existing scope values, and resolve duplicates before applying constraints. See the [implementation record](../exec-plans/completed/identity-multi-tenant-rbac-models.md) for acceptance scenarios and completion details.
