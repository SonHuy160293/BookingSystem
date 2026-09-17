# Identity Tenant CRUD and paging APIs

Status: Completed (source implementation; executable verification omitted by request)
Date: 2026-10-08

## Implementation

Implemented using [add-cqrs-api-feature](../../../.codex/skills/add-cqrs-api-feature/SKILL.md), following the existing Branch features. TenantsController uses the versioned ApiController route and ISender for four endpoints:

| Method | Route | Result |
| --- | --- | --- |
| POST | `/api/v1/tenants` | 201 TenantDto with get-by-ID Location |
| PUT | `/api/v1/tenants/{id:guid}` | 200 TenantDto; 404 if missing |
| GET | `/api/v1/tenants/{id:guid}` | 200 TenantDto; 404 if missing |
| GET | `/api/v1/tenants` | 200 PagedResult of TenantDto |

Creation accepts Name and optional LegalName, TaxCode, ContactEmail, ContactPhone, and Address. Tenants start active with UUID v7 IDs. PUT replaces details and requires explicit IsActive; omitted optional strings are cleared. Domain detail normalization is shared with creation and validates every field before mutation. Activation/deactivation changes only the tenant flag.

CQRS validators enforce nonblank names, nonempty update/get IDs, IsActive presence, and mapped string lengths after trimming. No tenant-name or tax-code uniqueness policy is introduced. The existing validation and command transaction pipeline owns validation and persistence completion; handlers and repositories do not call SaveChangesAsync.

ITenantRepository uses the shared IRepositoryBase contract. TenantRepository uses EF Core for writes and no-tracking DTO reads; it is registered in Infrastructure DI. Paging searches Name, LegalName, TaxCode, and ContactEmail before counting and pagination. Sort fields are Name, LegalName, CreatedAt, IsActive, and Id with an Id tie-breaker. Missing or unsupported sort fields use Name ascending. Paging uses existing normalization (page 1, size 20, maximum 100), includes inactive tenants, and guards offset overflow.

## Acceptance scenarios (documented, not executed)

- Create a tenant with UUID v7, trimmed details, optional nulls, active status, and a Location pointing to get-by-ID.
- Update details and activate/deactivate a tenant while retaining its ID and creation metadata; omitted optional details become null.
- Reject blank Name and values exceeding Name/LegalName 250, TaxCode 50, ContactEmail 256, ContactPhone 32, or Address 500 characters.
- Reject empty GUIDs and omitted/null IsActive on PUT; accept explicit false.
- Return 404 for nonexistent tenants on update and get-by-ID.
- Return both active and inactive tenants and search each supported field, including rows with nullable optional values.
- Apply each supported ascending/descending sort, stable Id tie-breaks, and Name-ascending fallback.
- Normalize invalid page parameters, cap page size, and return correct totals and empty items for out-of-range or very large page numbers.
- Preserve existing RBAC and UserClaim implementation.

## Boundaries and review

No authorization attributes, permission-catalog changes, Dapper read repository, schema changes, migrations, tests, builds, or verification scripts are included. The user explicitly requested source/diff review only. No successful executable verification is claimed.

The Tenant table and existing mappings must be deployed separately before these APIs run against a database. Runtime tenant isolation and authorization remain separate tasks. Source review covers controller-to-CQRS flow, validation, domain mutation, repository DTO projection, DI registration, pagination overflow handling, and matching signatures.
