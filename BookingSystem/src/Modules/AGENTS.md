# AGENTS.md — Modules

Additional instructions for Codex working under `src/Modules/`.

Root `AGENTS.md` rules continue to apply.

Every bounded context lives here as its own module.

## Current modules

| Module   | Folder                    | Owns                                                      | Its AGENTS.md                                                        |
| -------- | ------------------------- | --------------------------------------------------------- | -------------------------------------------------------------------- |
| Identity | `BookingSystem.Identity/` | Accounts, roles, permissions, auth/tokens, avatar storage | [BookingSystem.Identity/AGENTS.md](BookingSystem.Identity/AGENTS.md) |
| Order | `BookingSystem.Order/` | Order capabilities | [BookingSystem.Order/AGENTS.md](BookingSystem.Order/AGENTS.md) |
| Inventory | `BookingSystem.Inventory/` | Inventory capabilities | [BookingSystem.Inventory/AGENTS.md](BookingSystem.Inventory/AGENTS.md) |
| Payment | `BookingSystem.Payment/` | Payment capabilities | [BookingSystem.Payment/AGENTS.md](BookingSystem.Payment/AGENTS.md) |
| Cart | `BookingSystem.Cart/` | Cart capabilities | [BookingSystem.Cart/AGENTS.md](BookingSystem.Cart/AGENTS.md) |

Identity, Order, Inventory, Payment, and Cart are the current modules.

Do not assume additional modules such as Catalog exist because documentation uses other domains as CQRS examples. Those examples are illustrative unless the module appears in the table above.

## Adding a new module

Follow the structure of an existing module rather than inventing a new layout:

```text id="gq5oz5"
src/Modules/BookingSystem.<Name>/
├── BookingSystem.<Name>.Api
├── BookingSystem.<Name>.Application
├── BookingSystem.<Name>.Domain
├── BookingSystem.<Name>.Infrastructure
├── BookingSystem.<Name>.MigrationRunner
└── BookingSystem.<Name>.Worker          # optional
```

`Worker` is optional. Add it only when the module requires background processing, scheduled jobs, queue consumers, or similar hosted workloads.

When adding a module:

1. Add the required projects to `BookingSystem.slnx`.
2. Add new package versions to `Directory.Packages.props`.
3. Create the module's own `AGENTS.md`.
4. Add the appropriate test projects following `tests/AGENTS.md`.
5. Add the module to the table above.
6. Add a `Worker` project only when the module requires one.

If a Codex `add-new-module` skill exists under `.codex/skills/`, use it for the end-to-end workflow.

## Shared persistence contracts

Reuse `IRepositoryBase<TEntity, TId>`, `IDeletableRepository<TEntity, TId>`, and
`IReadRepositoryBase<TDto, TId, TRequest>` from
`BookingSystem.SharedKernel.Abstractions.Persistence`.

Do not recreate these generic contracts inside a module. Keep domain-specific repository interfaces
in the owning module's Application project and EF Core implementations in its Infrastructure project.

## Module instructions

Every real module must have its own `AGENTS.md`.

Use `BookingSystem.Identity/AGENTS.md` as the reference structure and adapt its layer-specific rules to the new domain.

Do not omit the module-level `AGENTS.md`; without it, Codex falls back to the broader repository rules and loses module-specific boundaries.
