# Repository Investigation

Use the cheapest deterministic source that can answer the question. Expand context only when needed.

## Escalation order

1. **Known path** -> open the exact file.
2. **Known symbol/text** -> use `rg` or exact search.
3. **Unknown relationship/behavior** -> use CodeGraph when available.
4. Inspect the symbols/files returned by discovery.
5. Use `rg` to verify related usages and contracts.
6. Expand to neighboring projects/modules only when evidence requires it.

For non-trivial changes, inspect the closest existing implementation and its tests before introducing a new pattern.

Do not repeatedly read files already understood, and do not scan an entire repository when targeted retrieval is sufficient.

## Prefer `rg` for

- exact symbols;
- filenames;
- route names;
- configuration keys;
- literal usages.

Examples:

```bash
rg "CreateUserCommand" src/
rg "IUserRepository" src/
rg "AddIdentityInfrastructure" src/
```

## Prefer CodeGraph for

- call paths;
- callers/callees;
- dependency relationships;
- architecture exploration;
- change/blast-radius analysis;
- locating behavior when the exact symbol is unknown.

Treat CodeGraph results as discovery candidates, not source-of-truth behavior. Verify important conclusions against actual source before editing.

When a `.codegraph/` directory exists, CodeGraph is available for structural investigation.

MCP tool when available:

```text
codegraph_explore
```

Shell fallback:

```bash
codegraph explore "<symbol names or question>"
```

If `.codegraph/` does not exist, skip CodeGraph entirely. Indexing is the user's decision.

## Progressive disclosure

Do not front-load every architecture document into context. Start from root `AGENTS.md`, follow the nearest scoped guidance, then load only the documents necessary to resolve the task.

The goal is not maximum context. The goal is sufficient, current, verifiable context.
