# Cinema catalog models

Cinema now reuses Identity Branch.Id as its primary key; the separate BranchId column was removed. See [shared venue identity](cinema-branch-shared-identity.md) for the current design. The implementation and verification below are historical.

The original chain-profile ownership below was superseded by [tenant branding consolidation](tenant-chain-branding.md). The current Cinema model has five tables; Identity Tenant owns chain branding. The verification results below describe the initial implementation before that consolidation.

## Outcome and boundaries

Implement the approved Region, Genre, Film, FilmGenre, CinemaChain, and Cinema domain models, PostgreSQL Fluent mappings, and existing DbContext registrations. Use the [create-ef-entity workflow](../../../.codex/skills/create-ef-entity/SKILL.md) and [Cinema module instructions](../../../src/Modules/BookingSystem.Cinema/AGENTS.md).

No migrations, seed data, import adapters, external identity tracking, repositories, HTTP APIs, scheduling, reviews, or pricing are included.

## Accepted design

- Tables use the `cinema` schema and lowercase names; all columns use PascalCase.
- Region, Genre, and Film are shared catalog data. Region and Genre use locally assigned integer keys without audit inheritance. FilmGenre is an unaudited composite-key association.
- Film and Cinema generate UUID v7 identities. CinemaChain maps its inherited Id to TenantId and receives the existing Identity tenant UUID.
- Cinema owns public chain branding and venue presentation data. Identity retains administrative Tenant and Branch ownership. Only local Region, Genre, Film, and CinemaChain foreign keys are mapped.
- A globally unique Cinema BranchId allows at most one venue per Identity branch. Future write workflows must validate the branch's tenant ownership externally.
- Genres are relational. Country codes, format tags, and caption options are normalized descriptive JSONB lists, with no inferred screening combinations.
- Existing audit conventions and interceptors are reused for Film, CinemaChain, and Cinema. Reference records and associations have no audit fields.

## Implementation and verification

1. Add guarded, persistence-ignorant domain models and local status enums.
2. Add six configurations with explicit indexes, constraints, deletion rules, and native JSONB primitive collections.
3. Register the six DbSets through the existing context and assembly configuration scanning.
4. Build the affected projects, inspect generated model SQL, and check model metadata/domain invariants without migrations or a database connection.

Progress: complete.

## Verification results

- Cinema Infrastructure build, including Domain and its existing dependencies: passed with zero warnings and zero errors.
- Temporary, uncommitted model inspection: passed. EF exposes exactly six intended tables, PascalCase columns, no shadow properties or cross-module foreign keys, nine indexes, four local foreign keys, and nine check constraints.
- Generated PostgreSQL create SQL was inspected without executing it or creating migrations. It includes JSONB array defaults/checks, named keys/indexes, coordinate/status checks, and the planned restrict/cascade deletion rules.
- Sample runtime/release date, duplicate-synopsis removal, UUID identities, slug/list normalization, genre deduplication and retained association identities, read-only list views, provider JSON serialization/deserialization, and EF array change tracking were checked successfully.
- Invalid runtime/reference IDs, malformed country codes, invalid statuses, and invalid/partial coordinate updates are rejected without partially updating mutable records.
- Scoped diff and new-source whitespace checks passed. UTF-8 and CRLF were normalized for the owned files.

The temporary inspection executable was aligned with the existing Cinema API's private EF Design dependency to resolve its Relational assembly version; production dependency files were not changed. No test infrastructure was committed.

## Follow-up

Database rollout, reference-data population, tenant authorization, branch ownership integration, and actual database round-trip/constraint verification require separate work. No local foreign key proves that an Identity tenant or branch exists.
