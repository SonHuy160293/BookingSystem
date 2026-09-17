# AGENTS.md — BookingSystem

Instructions for OpenAI Codex working in this repository.

Read this file before making changes anywhere in the solution. Treat it as the repository map and global operating contract, not as the complete engineering manual.

Directories may contain their own `AGENTS.md`. Root instructions continue to apply throughout the repository unless a more deeply nested `AGENTS.md` overrides a conflicting instruction for its subtree.

## 1. Repository map

BookingSystem is a modular-monolith backend built with ASP.NET Core and Clean Architecture.

Bounded contexts live under `src/Modules/<ModuleName>`, normally split into `Api / Application / Domain / Infrastructure / Worker / MigrationRunner` projects.

Current modules are **Identity, Order, Cinema, Payment, and Cart**. Verify a module exists in source before modeling code after reference material.

| Area | Path | Local guidance |
| --- | --- | --- |
| Shared technical primitives | `src/BuildingBlocks/` | `src/BuildingBlocks/AGENTS.md` |
| Local orchestration | `src/BookingSystem.AppHost/` | `src/BookingSystem.AppHost/AGENTS.md` |
| Cross-module DTOs & events | `src/Contracts/` | `src/Contracts/AGENTS.md` |
| Bounded-context modules | `src/Modules/` | `src/Modules/AGENTS.md` |
| Identity module | `src/Modules/BookingSystem.Identity/` | `src/Modules/BookingSystem.Identity/AGENTS.md` |
| Tests | `tests/` | `tests/AGENTS.md` |
| Deployment | `deploy/` | [docs/development/deployment.md](docs/development/deployment.md) |
| Architecture | repository-wide | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Runtime / Aspire | repository-wide | [docs/architecture/runtime-orchestration.md](docs/architecture/runtime-orchestration.md) |
| Verification | repository-wide | [docs/development/verification.md](docs/development/verification.md) |
| Investigation tools | repository-wide | [docs/development/repository-investigation.md](docs/development/repository-investigation.md) |
| Plans | repository-wide | [docs/PLANS.md](docs/PLANS.md) |
| Quality / drift control | repository-wide | [docs/QUALITY.md](docs/QUALITY.md) |
| Architecture decisions | repository-wide | [docs/decisions/README.md](docs/decisions/README.md) |

The documentation index and ownership rules live in [docs/README.md](docs/README.md).

## 2. Global invariants

These rules apply everywhere unless a deeper `AGENTS.md` explicitly narrows them without violating repository-wide architecture.

### Toolchain and code style

- .NET 10; SDK version is pinned in `global.json`.
- C# latest, nullable reference types enabled, implicit usings enabled.
- NuGet versions are centrally managed in `Directory.Packages.props`. Project files use bare `PackageReference` entries without `Version` attributes.
- Build-time analyzers and `.editorconfig` are the source of truth for naming and formatting.
- A change that only satisfies the IDE but not the build is not valid.

### Dependency boundaries

The repository follows this dependency direction:

```text
Module.Api             -> Module.Application + Module.Infrastructure + Contracts + BuildingBlocks
Module.Infrastructure  -> Module.Application + Module.Domain + Contracts + BuildingBlocks
Module.Application     -> Module.Domain + Contracts + BuildingBlocks
Module.Domain          -> BuildingBlocks only
Contracts              -> BuildingBlocks only when needed
BuildingBlocks         -> other BuildingBlocks projects, one-way, no cycles
```

**Application must never reference Infrastructure.**

Controllers must not inject `DbContext`, `UserManager`, `RoleManager`, repositories, or domain/business services directly. Controllers communicate with Application through `ISender`.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the reasoning, request pipeline, and persistence boundaries.

### CQRS and persistence

This repository does **not** use MediatR. Do not add `MediatR`, `IRequest`, or `IRequestHandler`.

Use the mediator abstractions from `BookingSystem.SharedKernel`, including `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, command/query handlers, and `IValidator<TRequest>`.

Commands use the transaction/unit-of-work pipeline. Handlers and repositories must not call `SaveChangesAsync` when that pipeline owns persistence completion. Queries do not open command transactions.

Unhandled `DomainException`-derived exceptions are translated by `GlobalExceptionHandlingMiddleware`; controllers should not add broad `try/catch` blocks around normal application flow.

## 3. Work protocol

### Discover before editing

Use progressive disclosure. Do not load broad repository context by default.

1. Read the nearest applicable `AGENTS.md`.
2. If the path or symbol is known, inspect the exact file or use exact search.
3. If relationships or behavior are unclear, use CodeGraph when available.
4. Inspect the closest existing implementation and tests before introducing a new pattern.
5. Verify important conclusions against source code before editing.
6. Expand to neighboring modules only when evidence requires it.

Detailed tool guidance lives in [docs/development/repository-investigation.md](docs/development/repository-investigation.md).

### Plan proportionally

For small, explicit, low-risk tasks, normalize the request internally and proceed.

For complex work, create a lightweight plan. Use a checked-in execution plan when the change is long-running, cross-cutting, risky, or likely to require progress/decision tracking. See [docs/PLANS.md](docs/PLANS.md).

Ask for human judgment before implementation when the request is materially ambiguous, destructive, security-sensitive, or presents materially different product/architecture choices. Do not block on approval for routine implementation details already determined by repository conventions.

Do not silently expand scope.

## 4. Verification contract

Every code change receives the **minimum relevant feedback loop by default**. Examples:

- handler/application logic -> targeted unit tests;
- persistence/repository/migration -> targeted integration checks where practical;
- controller/API behavior -> targeted API/end-to-end checks where practical;
- architecture dependency changes -> architecture tests;
- documentation-only work -> no build/test unless needed.

During implementation, prefer the smallest useful loop: change -> targeted build/test -> diagnose -> fix -> rerun.

Run the full repository verification workflow when the user explicitly requests full verification or when the accepted plan identifies it as necessary for a high-risk/cross-cutting change.

The authoritative verification procedure and failure policy are in [docs/development/verification.md](docs/development/verification.md).

Never report a verification step as successful unless it actually ran successfully. If an environment limitation or pre-existing failure blocks verification, report exactly what was not verified and why.

## 5. Repository knowledge is the source of truth

Prefer repository-local, versioned knowledge over assumptions from memory, chat history, or external reference material.

- Architecture and dependency rules -> [ARCHITECTURE.md](ARCHITECTURE.md)
- Runtime topology / Aspire / observability boundaries -> [docs/architecture/runtime-orchestration.md](docs/architecture/runtime-orchestration.md)
- Verification -> [docs/development/verification.md](docs/development/verification.md)
- Deployment and environment handling -> [docs/development/deployment.md](docs/development/deployment.md)
- Plans and execution history -> [docs/PLANS.md](docs/PLANS.md) and `docs/exec-plans/`
- Durable architecture decisions -> `docs/decisions/`
- Quality principles and mechanical-enforcement targets -> [docs/QUALITY.md](docs/QUALITY.md)
- Sample/reference material -> `docs/references/`; verify it against current source before treating it as implementation truth.

When code and documentation disagree, verify actual behavior, then update stale documentation as part of the change when appropriate.

## 6. Skills

Repository-specific reusable Codex workflows live under `.codex/skills/`.

Use a skill when the task matches its purpose. Skills define repeatable procedures; they do not replace repository-wide invariants or architecture documentation.

Do not duplicate large sections of this file inside skills. Skills should point to the relevant repository sources of truth and add only task-specific workflow.

See `.codex/skills/SKILLS-PLAN.md` when present.

## 7. Evolving the harness

When an agent repeatedly fails for the same reason, do not solve it only with a larger prompt. Identify the missing capability and prefer, in order:

1. clearer repository-local documentation;
2. a reusable skill or script for a repeatable procedure;
3. a test, analyzer, linter, or CI check for an invariant that can be enforced mechanically.

Keep hard boundaries strict and allow implementation freedom inside them.

When `src/ApiGateway/` or `src/Shared/` receives its first real project, add scoped `AGENTS.md` guidance based on its actual responsibilities instead of inventing detailed rules in advance.
