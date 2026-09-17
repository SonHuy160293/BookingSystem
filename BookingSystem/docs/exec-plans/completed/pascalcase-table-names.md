# PascalCase table names

## Scope

Use plural PascalCase names for application database tables. Update the Cinema,
Order, Payment and shared reliability EF mappings; Identity already follows this
convention. Preserve schemas, columns, relationships and explicit constraint names.
Update current documentation and the create-ef-entity skill.

## Progress

- [x] Rename table literals consistently in configurations and mapping-helper calls.
- [x] Update repository guidance, current documentation and entity skill.
- [x] Review changed source and record deployment requirements.

## Verification and deployment

Source/diff review only. No restore, build, tests, formatter, Docker execution,
seed data, migration creation or database operations. Historical migrations,
snapshots and completed execution plans remain historical records. Existing
databases require separate rename migrations before using the updated mappings;
PostgreSQL PascalCase table identifiers must be quoted.

## Completion

Renamed 27 distinct table mappings across 25 configuration files: 11 Cinema,
8 Order, 5 Payment and 3 shared reliability tables. Mapping-helper calls and
ToTable literals agree. Identity's existing PascalCase mappings were retained.
Source/diff review found no remaining lowercase runtime table mappings in these
areas. No executable verification or database changes were performed.
