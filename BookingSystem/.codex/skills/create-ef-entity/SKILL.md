---
name: create-ef-entity
description: Create or update an EF Core entity mapping for a BookingSystem module. Use when Codex is asked to add an entity/model that maps to a database table, create its IEntityTypeConfiguration mapping, and register it in that module's DbContext. Typical triggers include "create entity for this table", "map this SQL table", "add EF model", or "add entity configuration". This skill is repository-specific and must follow the applicable AGENTS.md, .editorconfig, and prompt-selected verification mode.
---

# Create EF Entity

Create one database-mapped entity through the repository's standard three-step flow:

1. Domain model
2. EF Core configuration
3. Module DbContext registration

Do not create migrations, repositories, DTOs, commands/queries, controllers, or tests unless the user's prompt explicitly requests them.

## 1. Load repository rules first

Before editing:

1. Read the root `AGENTS.md`.
2. Read `src/Modules/AGENTS.md`.
3. Read the target module's `AGENTS.md`.
4. Follow `.editorconfig` for naming and formatting.
5. Follow the root prompt-selected verification mode. Do not force the full verification loop from this skill.

Repository instructions override examples in this skill.

## 2. Identify the target module and schema

Resolve:

- module name;
- entity/model name;
- database table name and schema;
- primary key;
- columns, SQL types, nullability, lengths, precision, defaults;
- indexes and unique constraints;
- foreign keys and relationships.

Prefer authoritative information in this order:

1. schema/DDL explicitly supplied by the user;
2. existing migrations or database definitions in the repository;
3. existing mappings for the same table/domain;
4. user-described fields.

Do not invent database constraints or relationships when they cannot be established safely.

Inspect one or two existing entities and configurations in the same module before creating new code. Follow their conventions rather than introducing a new pattern.

## 3. Create the Domain model

Create:

```text
src/Modules/BookingSystem.<Module>/
  BookingSystem.<Module>.Domain/
    Models/<Entity>.cs
```

Use the namespace:

```text
BookingSystem.<Module>.Domain.Models
```

Follow existing module conventions for:

- base entity type;
- ID type;
- audit interfaces/base classes;
- soft-delete interfaces/base classes;
- navigation properties;
- constructors and property setters.

Keep the model persistence-ignorant:

- no EF Core attributes;
- no `DbContext` references;
- no Infrastructure references;
- no table/column attributes when Fluent configuration can express the mapping.

Model SQL nullability correctly with C# nullable reference/value types.

## 4. Create the EF Core configuration

Create:

```text
src/Modules/BookingSystem.<Module>/
  BookingSystem.<Module>.Infrastructure/
    Persistence/Configurations/<Entity>Configuration.cs
```

Use the module's established configuration namespace, normally:

```text
BookingSystem.<Module>.Infrastructure.Persistence.Configurations
```

Implement:

```csharp
IEntityTypeConfiguration<<Entity>>
```

Configure only behavior supported by the schema and repository conventions, including when applicable:

- `ToTable` and schema;
- primary key;
- column names;
- required/optional fields;
- maximum lengths;
- SQL column types when necessary;
- precision/scale;
- generated/default values;
- indexes;
- unique indexes;
- foreign keys;
- navigation relationships;
- delete behavior;
- concurrency properties.

Do not duplicate audit/soft-delete configuration already supplied by shared conventions or interceptors unless existing module mappings explicitly do so.

Do not place business rules in EF configuration.

## 5. Register the entity in the module DbContext

Locate the target module's existing DbContext under its Infrastructure persistence area. Normally this is:

```text
BookingSystem.<Module>.Infrastructure/
  Persistence/DbContexts/ApplicationDbContext.cs
```

Do not create a second DbContext when the module already has one.

Add the entity using the module's established pattern, normally:

```csharp
public DbSet<<Entity>> <Entities> => Set<<Entity>>();
```

Ensure the configuration is discovered by the DbContext's existing configuration-loading mechanism. If the context already uses assembly scanning such as `ApplyConfigurationsFromAssembly`, do not manually register the configuration again.

## 6. Check the mapping as one unit

Before finishing, inspect the model, configuration, and DbContext registration together and confirm:

- property types match the database columns;
- nullability matches;
- PK/FK types match;
- table/schema names are correct;
- lengths and precision match;
- indexes/uniqueness match;
- navigation relationships agree on both sides;
- no duplicate `DbSet` or configuration exists;
- no Domain-to-Infrastructure dependency was introduced.

If the user supplied a table definition, explicitly compare every mapped column against it before finishing.

## 7. Verification

Obey the verification mode in the user's prompt and root `AGENTS.md`.

- If full verification was requested, follow the repository's full verification loop.
- If targeted verification was requested, run only the relevant build/tests/checks.
- If no verification was requested, do not force `./scripts/verify.sh`; use only the minimum checks required by the root instructions.

Do not create a migration merely to verify the mapping unless the user explicitly asked for a migration.

## Completion report

Report briefly:

- entity created/updated;
- configuration created/updated;
- DbContext registration changed;
- any schema detail that could not be verified;
- verification actually performed.
