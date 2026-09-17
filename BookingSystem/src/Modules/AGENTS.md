# AGENTS.md — Modules

Additional instructions for Codex working under `src/Modules/`.

Root `AGENTS.md` rules continue to apply.

Every bounded context lives here as its own module.

## Current modules

| Module   | Folder                    | Owns                                                      | Its AGENTS.md                                                        |
| -------- | ------------------------- | --------------------------------------------------------- | -------------------------------------------------------------------- |
| Identity | `BookingSystem.Identity/` | Accounts, roles, permissions, auth/tokens, avatar storage | [BookingSystem.Identity/AGENTS.md](BookingSystem.Identity/AGENTS.md) |

Identity is currently the only real module.

Do not assume modules such as Catalog, Inventory, or Order exist because documentation uses `Product`, `Category`, or `Order` as CQRS examples. Those are illustrative examples only.

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

1. Add the required projects to `BookingSystem.sln`.
2. Add new package versions to `Directory.Packages.props`.
3. Create the module's own `AGENTS.md`.
4. Add the appropriate test projects following `tests/AGENTS.md`.
5. Add the module to the table above.
6. Add a `Worker` project only when the module requires one.

If a Codex `add-new-module` skill exists under `.codex/skills/`, use it for the end-to-end workflow.

## Module instructions

Every real module must have its own `AGENTS.md`.

Use `BookingSystem.Identity/AGENTS.md` as the reference structure and adapt its layer-specific rules to the new domain.

Do not omit the module-level `AGENTS.md`; without it, Codex falls back to the broader repository rules and loses module-specific boundaries.
