# AGENTS.md — BookingSystem

Instructions for OpenAI Codex working in this repository.

Read this file before making changes anywhere in the solution.

Directories may contain their own `AGENTS.md`. Root instructions continue to apply throughout the repository unless a more deeply nested `AGENTS.md` overrides a conflicting instruction for its subtree.

## 1. What this solution is

A modular-monolith backend built with ASP.NET Core and Clean Architecture.

Bounded contexts live under `src/Modules/<ModuleName>`, each split into `Api / Application / Domain / Infrastructure / Worker / MigrationRunner` projects.

Today there is one real module, **Identity** (`src/Modules/BookingSystem.Identity`) — accounts, roles, permissions, tokens, avatar storage.

The CQRS code samples elsewhere in this repo's docs (`Product`, `Category`, `Order`) illustrate the *pattern*, not a second module. Do not look for a Catalog module that does not exist yet.

| Area                        | Path                                  | Its own AGENTS.md                                                                            |
| --------------------------- | ------------------------------------- | -------------------------------------------------------------------------------------------- |
| Shared technical primitives | `src/BuildingBlocks/`                 | [src/BuildingBlocks/AGENTS.md](src/BuildingBlocks/AGENTS.md)                                 |
| Cross-module DTOs & events  | `src/Contracts/`                      | [src/Contracts/AGENTS.md](src/Contracts/AGENTS.md)                                           |
| Bounded-context modules     | `src/Modules/`                        | [src/Modules/AGENTS.md](src/Modules/AGENTS.md) — module map + how to add one                 |
| Identity module             | `src/Modules/BookingSystem.Identity/` | [src/Modules/BookingSystem.Identity/AGENTS.md](src/Modules/BookingSystem.Identity/AGENTS.md) |
| Gateway/BFF                 | `src/ApiGateway/`                     | none yet — empty scaffold, see §9                                                            |
| App-level shared code       | `src/Shared/`                         | none yet — empty scaffold, see §9                                                            |
| Tests                       | `tests/`                              | [tests/AGENTS.md](tests/AGENTS.md)                                                           |
| Deployment                  | `deploy/`                             | see §8                                                                                       |

## 2. Stack and non-negotiables

* .NET 10 (SDK pinned in `global.json`, currently `10.0.301`), C# latest, `Nullable` enabled, `ImplicitUsings` enabled.
* NuGet versions are centrally managed in `Directory.Packages.props`. Add a new package's version there once; project files only get a bare `<PackageReference Include="…" />` with no `Version` attribute.
* `EnforceCodeStyleInBuild` is on.
* `Microsoft.CodeAnalysis.NetAnalyzers`, `Roslynator.Analyzers`, and `StyleCop.Analyzers` run at build time.
* Naming and formatting conventions are defined in `.editorconfig`.
* Interfaces use `IPascalCase`.
* Types, methods, and properties use `PascalCase`.
* Private fields use `_camelCase`.
* Async methods end in `Async`.
* Use file-scoped namespaces.

A change that only satisfies the IDE but not the build is not valid.

### CQRS

**This project does not use MediatR.**

CQRS runs on the self-made mediator in `BookingSystem.SharedKernel`.

Use:

* `ICommand`
* `ICommand<TResponse>`
* `IQuery<TResponse>`
* `ICommandHandler<…>`
* `IQueryHandler<…>`
* `IValidator<TRequest>`

from `SharedKernel.Abstractions.Message`.

Do not add the `MediatR` package or introduce `IRequest` / `IRequestHandler`. They will not wire into the existing mediator.

## 3. Before implementing a request

Translate the user's request into a concrete engineering task before editing code.

For a small, explicit, low-risk task, normalize the request internally and proceed.

For a request that is ambiguous, architectural, destructive, security-sensitive, cross-module, or likely to have multiple materially different implementations:

1. restate it as a concise implementation prompt;
2. identify scope and non-goals;
3. state important assumptions;
4. define acceptance criteria;
5. identify the expected verification;
6. present materially different implementation choices when they exist;
7. ask the user to approve or adjust the plan before implementation.

Do not ask for approval for routine implementation details when the requested behavior and repository conventions already determine the solution.

Do not silently expand the user's scope.

## 4. Investigate efficiently before editing

Do not load broad repository context by default.

Start with the cheapest and most deterministic tool that can answer the question and expand only when needed.

Preferred escalation:

1. known path → open the exact file;
2. known symbol/text → `rg` / exact search;
3. unknown relationship or behavior → CodeGraph;
4. inspect returned symbols/files;
5. use `rg` to verify related usages;
6. expand to neighboring projects/modules only when evidence requires it.

### Prefer `rg` for

* exact symbols;
* filenames;
* route names;
* configuration keys;
* literal usages.

Examples:

```bash
rg "CreateUserCommand" src/
rg "IUserRepository" src/
rg "AddIdentityInfrastructure" src/
```

### Prefer CodeGraph for

* call paths;
* callers/callees;
* dependency relationships;
* architecture exploration;
* change/blast-radius analysis;
* locating behavior when the exact symbol is unknown.

Treat CodeGraph results as discovery candidates, not source-of-truth behavior.

Verify relevant conclusions against the actual source before editing.

For non-trivial changes, inspect the closest existing implementation and its tests before introducing a new pattern.

Do not repeatedly read files already understood.

Do not scan an entire module or repository when targeted retrieval is sufficient.

## 5. The dependency law

```text
Module.Api             -> Module.Application + Module.Infrastructure + Contracts + BuildingBlocks
Module.Infrastructure  -> Module.Application + Module.Domain + Contracts + BuildingBlocks
Module.Application     -> Module.Domain + Contracts + BuildingBlocks
Module.Domain          -> BuildingBlocks only
Contracts              -> BuildingBlocks only when needed
BuildingBlocks         -> other BuildingBlocks projects, one-way, no cycles
```

**`Application` must never reference `Infrastructure`.**

This is the rule most worth protecting. It keeps handlers testable without a database and keeps persistence swappable.

Controllers must never inject:

* `DbContext`;
* `UserManager` / `RoleManager`;
* repositories;
* domain/business services directly.

Controllers communicate with Application through `ISender`.

If you are about to add an Infrastructure `using` to an Application file, or a repository to a controller constructor, stop and re-read `src/Modules/BookingSystem.Identity/AGENTS.md` (§Application / §Api).

There is usually a handler-shaped way to implement the requirement instead.

This rule should also be enforced mechanically by architecture tests. A dependency violation should fail `dotnet test` before reaching code review.

## 6. Request pipeline

Do not reorder this pipeline without an explicit architectural reason.

```text
Controller -> ISender.SendAsync(command/query)
  -> ValidationPipelineBehavior
  -> LoggingBehavior
  -> PerformancePipelineBehavior
  -> TracingPipelineBehavior
  -> TransactionPipelineBehavior   (commands only, via IUnitOfWork)
  -> CommandHandler / QueryHandler
  -> Result / Result<T>
```

Commands run inside a transaction that commits once after the handler returns.

Handlers and repositories must **not** call `SaveChangesAsync` when the transaction/unit-of-work pipeline owns persistence completion.

Queries do not open command transactions.

Unhandled `DomainException`-derived exceptions are translated to the standard `ApiResponse` error envelope by `GlobalExceptionHandlingMiddleware`.

Controllers should let these exceptions bubble instead of adding broad `try/catch` blocks.

## 7. Verification

Verification depth is controlled by the user's prompt.

Do not automatically run the full verification loop for every task.

### When the prompt requests the verification loop

If the user explicitly asks to:

* run the verification loop;
* fully verify the change;
* run regression tests;
* verify before completion;
* or otherwise requests full validation;

use the full agentic verification loop.

### Step 1 — Determine required tests

Determine the tests required by the change:

* handler/application logic → unit tests;
* persistence/repository/migration → integration tests;
* controller/API changes → end-to-end/API tests;
* architecture changes → architecture tests.

Add or update tests for new or changed behavior where appropriate.

For bug fixes, prefer reproducing the bug with an existing or new regression test before implementing the fix when practical.

### Step 2 — Use a fast inner loop

During implementation, use the smallest relevant feedback loop:

```text
implement
    ↓
build / targeted test
    ↓
failure?
    ├─ yes -> diagnose -> fix -> rerun targeted test
    └─ no  -> continue
```

Do not repeatedly run the complete regression suite while diagnosing a localized failure when a targeted test provides sufficient feedback.

### Step 3 — Run full verification

Once implementation is stable, run from the repository root:

```bash
./scripts/verify.sh
```

The full verification script runs:

1. restore + build;
2. `dotnet format --verify-no-changes`;
3. unit tests;
4. architecture tests;
5. integration tests;
6. end-to-end/API tests.

Integration tests may require Docker and Testcontainers.

### Step 4 — Handle failures

If a verification stage fails:

1. diagnose the root cause;
2. make the smallest corrective change;
3. rerun the targeted failing test/stage;
4. repeat until green;
5. rerun the full `./scripts/verify.sh` after the implementation is stable.

Do not:

* disable valid tests;
* delete valid tests;
* weaken assertions;
* change expected values merely to hide a defect.

Only report full verification success when `./scripts/verify.sh` actually exits successfully.

If verification cannot complete because of an environment limitation or a pre-existing failure, report exactly what was not verified and why.

### When the prompt does not request the verification loop

Do not run the full `./scripts/verify.sh` automatically.

Use only the minimum checks necessary to safely perform the requested task.

Examples:

```text
documentation-only change
-> no build/test unless requested

configuration inspection
-> no test run unless needed

localized code edit
-> targeted build/test when useful

investigation/planning task
-> do not modify code or run regression tests unless requested
```

Do not turn every coding task into a full verification run.

The user's prompt determines whether full verification is required.

## 8. Deployment and environment files

`deploy/compose/` holds local Docker Compose stacks:

* `docker-compose.yaml`
* `docker-compose.override.yaml`
* `docker-compose.infrastructure.yaml`

`deploy/env/` holds environment configuration.

Treat `.prod.env` as read-only unless the user explicitly asks to change it.

Do not "fix" production configuration as a side effect of an unrelated feature task.

If `.prod.env` contains real credentials or secrets, it must not be committed.

`docs/references/` contains sample/reference material and is not part of the build.

Do not treat code found there as the current implementation unless verified against source.

## 9. When `ApiGateway/` or `Shared/` get their first real project

Both are intentionally empty scaffolds today.

When either gets its first real project, give it its own `AGENTS.md`.

For `Shared/`, define rules based on its actual responsibilities.

For `ApiGateway/`, define gateway/BFF-specific concerns such as:

* routing;
* aggregation;
* authentication forwarding;
* downstream communication.

Do not invent detailed rules for an empty folder now.

Add scoped instructions when there is real content to govern.

## 10. Codex Skills

Repository-specific reusable Codex workflows live under:

```text
.codex/skills/
```

Skills complement this file; they do not replace repository-wide rules.

Use a relevant skill when the task matches its purpose.

Examples include:

* adding a CQRS feature;
* running the verification workflow;
* creating a migration;
* reviewing an EF Core query.

`AGENTS.md` defines constraints that always apply.

Skills define reusable procedures for specific tasks.

Do not duplicate large portions of this file inside a skill.

A skill should reference repository conventions and contain only the additional workflow required for its task.

See `.codex/skills/SKILLS-PLAN.md` for the current skill plan when present.

<!-- CODEGRAPH_START -->

## CodeGraph

When a `.codegraph/` directory exists, CodeGraph is available for structural repository investigation.

Prefer CodeGraph for:

* call paths;
* callers/callees;
* dependency relationships;
* architecture exploration;
* blast-radius analysis;
* locating behavior when the exact symbol is unknown.

Prefer `rg` for:

* known symbols;
* exact strings;
* filenames;
* routes;
* configuration keys;
* literal usages.

MCP tool when available:

```text
codegraph_explore
```

Shell fallback:

```bash
codegraph explore "<symbol names or question>"
```

Treat CodeGraph output as discovery context.

Verify important conclusions against the actual source before editing.

If `.codegraph/` does not exist, skip CodeGraph.

<!-- CODEGRAPH_END -->
