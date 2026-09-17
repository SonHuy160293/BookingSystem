# Cinema and Branch shared identity

## Outcome

Identity Tenant represents the operator (CGV), and Identity Branch represents its physical location (CGV Aeon Mall). Cinema remains the Cinema-owned venue profile and now uses the existing Branch.Id as Cinema.Id. Its factory requires the branch identifier instead of generating another UUID. TenantId remains immutable.

Removed the redundant Cinema.BranchId property, PostgreSQL column, and unique index. The Cinema primary key enforces at most one profile per Branch. Existing DbSet registration and configuration scanning remain valid. No cross-module database foreign key, navigation, or project dependency is introduced.

Cinema owns public venue name, address, region, coordinates, and availability. Identity Branch retains administrative fields and existing authorization references. Tenant continues to own chain branding. Future application workflows must validate Branch existence and tenant ownership through a trusted boundary; the shared key alone cannot enforce these across module databases.

## Reference review and verification

- Repository search found no Cinema factory callers requiring changes. Identity BranchId authorization and administrative references remain valid.
- Updated both module guidance files and marked earlier implementation records as superseded.
- Cinema Infrastructure and its Domain/Application dependencies built with zero warnings and errors.
- Temporary inspection verified supplied identity retention, rejection of empty tenant/branch IDs, immutable ownership during detail updates, five EF entities, no redundant or shadow BranchId, non-generated primary key, and only the local Region foreign key.
- Generated PostgreSQL create SQL contains no BranchId column or obsolete index. Inspection did not connect to a database.

No migrations, database changes, API changes, or committed test infrastructure were added. Existing persisted Cinema rows would require an explicit data migration before adopting shared keys in a deployed database; that rollout is outside this change.
