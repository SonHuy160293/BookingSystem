# Tenant chain branding consolidation

Cinema now reuses Identity Branch.Id as its primary key; the separate BranchId column was removed. See [shared venue identity](cinema-branch-shared-identity.md) for the current design. The implementation and verification below are historical.

## Outcome

Move chain branding into Identity's existing Tenant rather than maintaining a second profile with the same tenant identity in Cinema. Reuse Tenant.Name and add optional Slug and LogoUrl. Remove the separate Cinema chain model, configuration, DbSet, navigation, and local tenant foreign key.

## Ownership and compatibility

- Identity owns chain branding and administrative tenant data. Cinema owns individual venue data.
- Cinema.TenantId and BranchId are scalar cross-module references. No Identity database foreign keys, entity navigations, or module project references are introduced.
- Tenant.Slug is a nullable varchar(250), normalized to lowercase and unique when supplied through SQL Server's filtered unique index. Existing callers can create tenants without branding. LogoUrl is nullable nvarchar(max); Name already exists.
- Tenant.Create accepts optional branding parameters; ChangeBranding validates before mutating. Existing ChangeDetails calls preserve branding.
- PascalCase column names and existing audit conventions are retained. Existing Tenant DbSet registration already discovers the updated mapping.

## Verification

- Cinema Infrastructure and its Domain dependency build passed with zero warnings/errors.
- Identity Infrastructure and its Domain/Application dependencies build passed with zero errors and 21 warnings in unrelated existing files.
- Temporary EF inspection passed: exactly five Cinema entities; no chain table/navigation or TenantId foreign key; Identity branding columns and filtered unique index match their planned SQL Server types.
- Legacy tenant creation, slug/logo normalization and clearing, rejected update atomicity, preservation during ordinary tenant detail changes, and EF change tracking passed inspection.
- Both generated create scripts were inspected without connecting to a database. Source search confirms all former chain-type/table references are removed from src.

No migrations, snapshot changes, HTTP API changes, seed/import logic, or committed test infrastructure were added. Actual SQL Server/PostgreSQL constraint execution and database rollout remain separate work.
