# Quality and Entropy Control

Agent throughput can amplify both good and bad repository patterns. Quality controls should make preferred patterns legible and repeated violations mechanically difficult.

## 1. Golden principles

These principles guide repository evolution without prescribing every implementation detail:

1. **Protect boundaries centrally.** Dependency direction, transaction ownership, package policy, and other repository invariants should be mechanically enforced where practical.
2. **Prefer existing repository primitives.** Before introducing a helper, abstraction, library, or pattern, inspect the closest established implementation and shared building blocks.
3. **Keep one source of truth.** Avoid copying architecture/workflow guidance across root instructions, skills, and docs.
4. **Make behavior verifiable.** New behavior should have an appropriate feedback path: tests, analyzers, runtime signals, or reproducible checks.
5. **Keep runtime boundaries observable.** Use structured logging, tracing, metrics, and health signals without leaking sensitive data.
6. **Do not hide defects.** Never weaken tests or expected values merely to make a check pass.
7. **Update the harness when failures repeat.** Repeated agent mistakes are signals to improve docs, skills/scripts, or mechanical enforcement rather than simply expanding prompts.
8. **Allow local freedom inside hard boundaries.** Enforce correctness, architecture, security, and reproducibility; avoid micromanaging harmless implementation style already governed by analyzers and `.editorconfig`.

## 2. Enforcement matrix

The repository should prefer the strongest practical enforcement mechanism for each rule.

| Invariant | Preferred mechanism |
| --- | --- |
| Application must not reference Infrastructure | architecture test |
| Domain dependency boundary | architecture test |
| Controllers must not depend directly on persistence/business implementations | architecture/structural test where practical |
| No MediatR in the current architecture | dependency/package check or architecture test |
| Naming/formatting | `.editorconfig` + analyzers |
| Central package versions | build/CI validation |
| Transaction completion ownership | targeted architecture/static test where practical |
| Secrets must not be committed | secret scanning / CI |
| Documentation links/structure remain valid | doc/link check when added |

This table is a target model, not a claim that every check already exists. Verify actual repository coverage before relying on it.

## 3. Drift and stale knowledge

When a reviewer or agent discovers that code behavior, architecture documentation, and instructions disagree:

1. verify the actual source/runtime behavior;
2. decide whether the code or documentation is stale;
3. fix the source of truth in the same change where practical;
4. add mechanical enforcement if the same class of drift is likely to recur.

## 4. Technical debt

Do not wait for a large cleanup event when debt can be removed in small safe increments.

Substantial known debt that should survive the current task can be tracked in an execution plan or a dedicated repository tracker when one exists. Avoid scattering long-lived TODO knowledge across chat history.

## 5. Quality review questions

For non-trivial changes, ask:

- Did this introduce a new pattern when an existing one was sufficient?
- Did it weaken a module or layer boundary?
- Is a rule being documented that should instead be tested/linted?
- Is important reasoning trapped only in the current conversation?
- Can the next agent reproduce and verify the behavior without relying on hidden context?
