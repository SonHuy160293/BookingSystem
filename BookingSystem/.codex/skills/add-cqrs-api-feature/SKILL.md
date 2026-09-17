---
name: add-cqrs-api-feature
description: Add or change an HTTP API endpoint in the BookingSystem modular Clean Architecture repository, end to end (controller, custom CQRS command/query, validator, handler, repository, response mapping). Use when asked to add, extend, or modify an API endpoint or use case.
---

# Add CQRS API Feature

Implement one API use case end to end while preserving existing architecture and conventions.

```text
HTTP request -> controller -> Sender (ISender) -> command/query -> validator
             -> handler -> repository (EF Core) or read repository (Dapper, optional)
             -> mapping -> HTTP response
```

Scope rule: add no layers, abstractions, packages, events, or database changes the requested behavior does not require. A missing file is not permission to redesign the module.

## 1. Load instructions

1. Read root `AGENTS.md`, `src/Modules/AGENTS.md`, and the target module's `AGENTS.md`.
2. Read `tests/AGENTS.md` only when tests or full verification are requested.
3. Follow `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`.
4. Follow the verification mode in the prompt (step 11).

Repository instructions override anything in this skill, including the examples.

## 2. Capture requirements

From the prompt, note what is stated and what is missing:

- module, HTTP method, route
- request fields and response contract
- read (query) or state change (command)
- expected errors: not found, conflict, validation, unauthorized, forbidden
- **read-side persistence instruction**: does the prompt ask for, forbid, or say nothing about Dapper / a read repository?
- verification mode

Query = read-only. Command = state change. Add an event only if another workflow must react after the primary use case succeeds.

Do not decide permissions, repository split, or mapping style yet. Those come from the code (step 3). If material behavior is still ambiguous after investigation, follow the root `AGENTS.md` prompt-approval rules before implementing.

## 3. Investigate

If `.codegraph/` exists, run `codegraph_explore` (MCP) or `codegraph explore "<feature, route, or symbol>"` (shell). If not, use targeted `rg` and say so in the report. Never invent graph findings.

Find the **reference feature**: the most similar existing endpoint in the same module (same kind, similar persistence). Record from it:

- controller shape, route and versioning convention, `ProducesResponseType` set
- `PermissionAuthorize(Functions.<Area>, ActionType.<Action>)` usage
- where request/response DTOs live
- command/query, validator, and handler shape
- which repository interfaces the handler uses; whether the module has a read repository at all
- mapping class and method (e.g. `MenuMapping.ToDto`)
- DI registration pattern; related tests
- callers and blast radius of anything you will modify

Verify graph findings against source. Keep searches at module/feature level.

Pipeline check: before writing a handler, confirm which behaviors are registered (validation, transaction/unit-of-work). This determines that handlers and repositories must not call `SaveChangesAsync`, and that validators run automatically.

## 4. Persistence policy (read this before designing anything)

The repository uses two separate persistence paths.

| | Write / domain repository | Read repository |
|---|---|---|
| Technology | EF Core | Dapper (`SqlConnectionFactory`) |
| Interface | `IXRepository : IRepositoryBase<TEntity>` | `IXReadRepository : IReadRepositoryBase<TDto, TRequest>` |
| Implementation | `XRepository : RepositoryBase<TEntity>` (takes `ApplicationDbContext`) | `XReadRepository : ReadRepositoryBase<TDto, TRequest>` |
| Status | **Mandatory** for every command and for any domain-entity access | **Optional**, only when justified below |
| Used by | command handlers (and query handlers only when Dapper is not used) | query handlers only |

**Write side (mandatory):**

- Every state change goes through a repository built on `RepositoryBase<TEntity>` via the EF change tracker. Never write with Dapper or raw SQL.
- Command handlers use the write repository even for their own lookups (e.g. `FindByCodeAsync`, `GetLevelAsync`, `HasCircularParentAsync`). Never use a read repository inside a command.
- New feature on an entity with no repository: create `IXRepository : IRepositoryBase<X>` in Application and `XRepository : RepositoryBase<X>` in Infrastructure, and register it. Add only the methods the feature needs.

**Read side (optional, decided in this order):**

1. Prompt explicitly asks for Dapper / a read repository: use it.
2. Prompt explicitly says not to (or says EF only): do not create or use one.
3. Prompt is silent: follow the reference feature of the same kind. If none exists or it does not use Dapper, do **not** introduce Dapper.
4. If Dapper would clearly be needed (e.g. a paged, searchable, sortable list that sibling features serve from a read repository) but the prompt does not say so, follow the approval rules in the root `AGENTS.md` rather than deciding silently.

Without a read repository, a query handler reads through the existing write repository: reuse a method, or add one focused read method that projects to a DTO, does not track, and filters/sorts/paginates before materialization. Do not create a read repository just to look symmetrical.

If the module already has a read repository for the entity, extend it instead of creating another. Never create a generic repository or BuildingBlock for one feature.

## 5. Contracts

Follow the pattern of the reference feature: a request DTO (`CreateMenuRequest`, `GetMenusRequest`) wrapped in the command/query (`CreateMenuCommand(CreateMenuRequest Request)`), and a response DTO (`MenuDto`). Place them where the reference feature places them. Use `BookingSystem.Contracts` only for contracts that cross module/service boundaries or where the module convention puts them.

Never expose EF entities or Infrastructure types. Changes to existing public contracts are compatibility-sensitive; flag them in the report.

## 6. Controller

Add the action to an existing suitable controller, or copy the nearest controller's structure. Use the base type matching the audience (`ApiController`, `PublicApiController`, `IntegrationApiController`).

An action binds input, sends via the base class `Sender`, and maps the result. No `DbContext`, repositories, EF Core, Infrastructure types, or business logic in controllers.

Required on every action:

- `[PermissionAuthorize(Functions.<Area>, ActionType.<Action>)]`, using existing `Functions`/`ActionType` values. Add a new `Functions` entry only when the feature introduces a new resource; follow the module's existing permission definition location.
- `[ProducesResponseType]` for every status the action can return (200/201, 400, 404, etc., matching the reference).
- `CancellationToken cancellationToken` passed to `Sender.SendAsync`.

Failure mapping: `result.IsSuccess ? <success> : HandleFailure(result)`. Do not hand-roll status codes.

Pattern for a read (list):

```csharp
[HttpGet]
[PermissionAuthorize(Functions.Menus, ActionType.View)]
[ProducesResponseType(typeof(PagedResult<MenuDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<PagedResult<MenuDto>>> GetMenus(
    [FromQuery] GetMenusRequest request, CancellationToken cancellationToken = default)
{
    var result = await Sender.SendAsync(new GetMenusQuery(request), cancellationToken);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
}
```

Pattern for a create:

```csharp
[HttpPost]
[PermissionAuthorize(Functions.Menus, ActionType.Create)]
[ProducesResponseType(typeof(MenuDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<MenuDto>> CreateMenu(
    [FromBody] CreateMenuRequest request, CancellationToken cancellationToken)
{
    var result = await Sender.SendAsync(new CreateMenuCommand(request), cancellationToken);
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetMenuById), new { id = result.Value.Id }, result.Value)
        : HandleFailure(result);
}
```

For 201 responses, `CreatedAtAction` must point to an existing get-by-id action. If it does not exist, follow the reference feature rather than inventing a broken link.

## 7. Command or query

Create under the module's feature-folder structure, matching the reference feature.

```text
Command: <Action><Area>Command, ...CommandValidator, ...CommandHandler
Query:   <GetSomething>Query, ...QueryValidator (if inputs need it), ...QueryHandler
```

Use the custom abstractions (`ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `IValidator<T>`). Never introduce MediatR. Do not manually register handlers or validators that assembly scanning discovers.

Rules:

- Message types are `sealed record`s wrapping the request DTO; handlers and validators are `sealed class`es with constructor-injected abstractions.
- Validator: `ValidateAsync(request, cancellationToken)` throws `CqrsValidationException([...])` for invalid input. Add one when inputs are validate-able or the module convention requires it. No empty validators; if omitted, say why in the report.
- Handler `HandleAsync` returns `Task<Result<T>>` (or `Task<Result>`). Failures that the module signals with `CqrsValidationException` (e.g. duplicate code) are thrown the same way; do not invent a different error style. Follow how the reference feature signals not-found.
- Handlers reference Application abstractions only: never `ApplicationDbContext`, `SqlConnectionFactory`, or Infrastructure types.
- Queries never mutate state.
- Commands rely on the transaction pipeline. No `SaveChangesAsync` in handlers or repositories (confirmed in step 3).
- Pass `CancellationToken` through every async call.

Command handler pattern:

```csharp
public sealed class CreateMenuCommandHandler : ICommandHandler<CreateMenuCommand, MenuDto>
{
    private readonly IMenuRepository _repository;
    public CreateMenuCommandHandler(IMenuRepository repository) => _repository = repository;

    public async Task<Result<MenuDto>> HandleAsync(CreateMenuCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (await _repository.FindByCodeAsync(request.Code, cancellationToken) is not null)
            throw new CqrsValidationException([$"Menu code '{request.Code}' already exists."]);

        var level = await _repository.GetLevelAsync(request.ParentId, cancellationToken);
        var menu = Menu.Create(/* domain factory, not property setters */);

        await _repository.AddAsync(menu, cancellationToken);   // tracked; pipeline commits
        return Result.Success(MenuMapping.ToDto(menu));
    }
}
```

Query handler pattern (Dapper read repository in use):

```csharp
public sealed class GetMenusQueryHandler : IQueryHandler<GetMenusQuery, PagedResult<MenuDto>>
{
    private readonly IMenuReadRepository _repository;
    public GetMenusQueryHandler(IMenuReadRepository repository) => _repository = repository;

    public async Task<Result<PagedResult<MenuDto>>> HandleAsync(GetMenusQuery query, CancellationToken cancellationToken)
        => Result.Success(await _repository.ListAsync(query.Request, cancellationToken));
}
```

Without a read repository, the same handler depends on the write repository interface and calls a focused read method that returns DTOs.

## 8. Implementing repositories

**Write repository (`RepositoryBase<T>`):**

- Constructor takes `ApplicationDbContext` and passes it to the base.
- Build queries with the base `Query()`; use `GetByIdAsync` from the base where it fits; mutate via domain methods (e.g. `menu.Disable()`), never by setting properties from outside.
- Add focused methods only (`FindByCodeAsync`, `HasChildrenAsync`, ...). No commit calls.
- Confirm from the base class whether `Query()` tracks entities and what `GetByIdAsync` does when missing; use `AsNoTracking()` on read-only paths that go through it.

**Read repository (`ReadRepositoryBase<TDto, TRequest>`, only when step 4 allows):**

- Constructor takes `SqlConnectionFactory` and passes it to the base.
- Override the base members the reference read repository overrides: `TableName`, `SelectColumns`, `DefaultOrderBy`, `NotFoundCode`, `EntityName`, `SortColumns`, `BuildWhereClause`.
- `SortColumns` is a whitelist mapping lower-case request keys to real column names (case-insensitive). Every sortable field must be listed there; never interpolate a request-supplied sort value into SQL.
- `BuildWhereClause` builds conditions with `DynamicParameters` and helpers like `BuildSearchCondition`. All values are parameters. Interpolate only constants you control (table/column names).
- Add extra methods (`ListAllAsync`, `ListVisibleAsync`) only when the feature needs them, using `QueryAsync<TDto>(sql, parameters, cancellationToken)` from the base and a `const` SQL string. Prefer reusing `SelectColumns` over copying the column list.
- Read-only. Never `INSERT`/`UPDATE`/`DELETE`.
- Extend the interface `IXReadRepository : IReadRepositoryBase<TDto, TRequest>` with any added method, and register the implementation via the module's DI extension.

## 9. Response mapping

Reuse the module's mapping class (static `XMapping.ToDto(...)` style). Do not add a mapping library. Dapper reads already return DTOs and need no entity mapping. No business rules in mapping.

## 10. Supporting concerns (only if required)

Errors/exceptions, events, options or external clients, indexes and persistence configuration. For a new entity/table mapping, use the `create-ef-entity` skill if available. Do not create or modify migrations unless the prompt or repository instructions require it. Note that a Dapper read repository needs the table/columns to exist already; do not assume unmapped columns.

## 11. Verification

Use the mode stated in the prompt; with no instruction, use the root `AGENTS.md` default.

| Mode | Do |
|---|---|
| Full | Add/update tests for changed behavior, iterate with targeted build/tests, run the full verification loop once stable, fix and rerun until green or an external blocker is identified. |
| Targeted | Run only the smallest relevant build/tests. No full script. Add tests only if requested or required by repo rules. |
| None | Run nothing and do not claim verification. Still review the diff for compile-time and architectural problems. |

Minimum tests when in scope: handler happy path; each expected failure (duplicate, not found, invalid parent); validator rules; permission check for protected endpoints; for Dapper reads, sort-field whitelist and search behavior. Follow `tests/AGENTS.md`.

## 12. Final review

Walk the flow once and confirm:

- controller depends only on `Sender`; has `PermissionAuthorize` and `ProducesResponseType`
- commands use only `RepositoryBase`-based repositories; nothing writes via Dapper
- Dapper read repository exists only if step 4 justified it
- Application does not reference Infrastructure
- no SQL built from unwhitelisted request input
- no EF entities in responses
- new implementations registered in DI
- no unrelated files or packages changed
- verification matches the requested mode

## Completion report

Briefly list: endpoint and route; command/query, validator, handler; write repository work; **read-side decision (Dapper used or not, and why)**; mapping used; components created because missing; reference feature used; CodeGraph use or fallback; tests and verification actually run; assumptions, risks, contract-compatibility concerns.