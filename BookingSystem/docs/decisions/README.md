# Architecture Decisions

Use this directory for durable decisions that future contributors/agents need to understand in order to avoid undoing an intentional design choice.

Do not create an ADR for routine implementation details. Use one when the decision changes or preserves a long-lived architectural constraint and the reason is not obvious from code alone.

Examples that may deserve a decision record:

- mediator strategy;
- module/database ownership;
- cross-module communication model;
- transaction/outbox strategy;
- multitenancy isolation model;
- authorization model;
- migration ownership;
- deployment topology.

## Suggested file naming

```text
0001-short-decision-title.md
0002-another-decision.md
```

## Suggested template

```markdown
# <Decision title>

Status: Proposed | Accepted | Superseded
Date: YYYY-MM-DD

## Context
What problem or constraint forced this decision?

## Decision
What did we choose?

## Why
Why is this preferable to the important alternatives?

## Consequences
What becomes easier, harder, required, or prohibited?

## Enforcement
Which tests, analyzers, CI checks, docs, or code structure protect the decision?

## Supersedes / Superseded by
Links when applicable.
```

When a decision becomes obsolete, preserve history by marking it superseded and linking to the replacement instead of silently rewriting the past.
