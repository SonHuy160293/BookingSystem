# Identity multi-tenant RBAC models and mappings

Status: Completed (source implementation; executable verification omitted by request)
Date: 2026-10-08

## Purpose and boundaries

Prepare Identity persistence for many tenants, each owning many branches, with role assignments at platform, tenant, or branch scope. Apply the `create-ef-entity` workflow to Domain models, EF configurations, DbContext registration, and the existing creation/assignment callers.

The user explicitly excludes migrations, builds, tests, and the verification loop. Do not change `UserClaim`, add tenant management APIs, reclassify the permission catalog, or implement runtime tenant isolation or authorization.

Sources of truth: [Identity guidance](../../../src/Modules/BookingSystem.Identity/AGENTS.md), [Domain guidance](../../../src/Modules/BookingSystem.Identity/BookingSystem.Identity.Domain/AGENTS.md), and [create-ef-entity](../../../.codex/skills/create-ef-entity/SKILL.md).

## Accepted decisions

The durable ownership and grant-scope rules are recorded in [Identity role scopes](../../decisions/0001-identity-role-scopes.md).

- `Tenant` inherits `Entity<Guid>` and generates UUID v7 IDs. Its required `Name` is `nvarchar(250)`. Optional tenant details are `LegalName` (`nvarchar(250)`), `TaxCode` (`varchar(50)`), `ContactEmail` (`nvarchar(256)`), `ContactPhone` (`varchar(32)`), and `Address` (`nvarchar(500)`). `IsActive` is required and initialized to true. Audit properties come from the base entity and existing conventions.
- Branch tenant ownership is required and remains unchanged by branch detail updates.
- Scope metadata uses the Domain enum `RbacScopeType`, stored as required `varchar(8)` values `PLATFORM`, `TENANT`, or `BRANCH`.
- Role `TenantId` describes definition ownership. Null means a reusable role definition available across tenants; its scope describes where it can be assigned. PLATFORM roles cannot have tenant ownership.
- UserRole tenant/branch IDs describe assignment scope: PLATFORM has neither ID, TENANT has only tenant ID, and BRANCH has both IDs. Tenant-owned roles cannot be assigned outside their tenant, and a branch must belong to the assignment tenant.
- UserRole has its own UUID v7 primary key without audit inheritance. Exact assignment uniqueness includes user, role, tenant, and branch, including null scope IDs.
- Role names are unique per owner tenant and scope type. Null ownership constitutes the reusable-role namespace. Nullable normalized names are excluded from the uniqueness rule.
- New tenant/branch relationships use `NoAction`. Composite branch assignment foreign keys enforce tenant ownership. Existing user/role deletion behavior remains in place.
- Existing role/permission factory callers default to PLATFORM. Permission synchronization without an explicit scope preserves the stored scope.

## Implementation slices

- [x] Add Tenant and scope enum; extend Branch, Role, PermissionDefinition, and UserRole domain state and validation.
- [x] Configure tenant table, scope storage, relationships, keys, indexes, and checks; register Tenants in the existing DbContext.
- [x] Update existing branch/role creation and role-assignment contracts, validators, handlers, and repository queries/projections.
- [x] Record completion and explicitly report the requested absence of executable verification.

## Acceptance scenarios (not executed)

- Tenant creation produces UUID v7, trims textual inputs, rejects blank names and values exceeding mapped lengths, and starts active.
- Branch creation requires a nonempty tenant ID; updating branch details preserves tenant ownership.
- A reusable role can be assigned to one user in multiple tenants or branches. An identical scoped assignment remains idempotent through the existing API, and the database unique index prevents duplicate rows.
- PLATFORM grants have no tenant/branch IDs; TENANT grants have only a tenant ID; BRANCH grants have both IDs. Invalid shapes, cross-tenant branch assignments, and assignments outside a role's owner tenant are rejected.
- Duplicate normalized role names conflict within the same owner and scope but may coexist across owners or scopes.
- Permission synchronization preserves an existing scope when scope is omitted.
- UserClaim and its EF configuration remain untouched.

## Database rollout and rollback

No database is changed by this work. Before deployment, a separately requested migration must create Tenants, backfill existing Branch tenant ownership with explicit tenant assignments, populate UserRole UUID keys, and assign existing Role/PermissionDefinition scope values. Resolve any duplicate names or assignments before applying uniqueness constraints.

Runtime authorization remains the existing JWT claim-based behavior; these mappings do not activate tenant isolation. Reverse only this change's source/doc edits to roll back, preserving unrelated working-tree changes. Any later database migration needs its own data-safe rollback procedure.

## Progress

- Accepted implementation plan supplied by the user on 2026-10-08.
- Repository and applicable Identity/Domain instructions reviewed. Implementation split into Domain, EF mappings, and existing callers.
- Tenant and scope models, mappings, keys, checks, and DbContext registration implemented. Scope validation uses domain-safe validation exceptions.
- Existing create-branch requests now require TenantId. Role creation requests default to uppercase PLATFORM and accept optional tenant ownership. Branch/role responses include their ownership and scope metadata.
- Role-name conflict queries compare ownership and scope. Assignment queries compare user, role, tenant, and branch; exact duplicates retain existing idempotent success behavior.
- Source review covered model/mapping agreement, nullable composite foreign keys, uniqueness filters, caller signatures, and existing DI registration. This is not an executable verification result.
- No migrations, tests, builds, or verification commands have been run.
