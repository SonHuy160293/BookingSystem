# Verification Workflow

Verification is a feedback loop, not a final ceremony.

Verification must reflect the capabilities that actually exist in the repository.

Do not assume that unit tests, integration tests, architecture tests, end-to-end tests, or a full verification script already exist. Verify the repository structure before attempting to use them.

Do not introduce new test infrastructure as a side effect of an unrelated task unless the user explicitly requests it or the accepted implementation plan includes it.

## 1. Default Minimum Verification

Every code change should receive the smallest relevant validation currently available in the repository, even when the user does not explicitly request full verification.

For the current project state, prefer:

- application or handler changes -> build the affected project/module;
- domain changes -> build the affected project/module;
- repository or persistence changes -> build the affected project/module and validate migrations/configuration where practical;
- controller/API changes -> build the affected API project and verify compilation/routing configuration where practical;
- migration changes -> verify the migration can be generated/applied using the repository's existing migration workflow where practical;
- dependency or architecture changes -> build affected projects and inspect dependency direction; use architecture tests only when they actually exist;
- formatting-sensitive changes -> use the repository's existing formatting/analyzer checks;
- documentation-only changes -> no build unless the change affects generated documentation or repository tooling;
- investigation/planning-only work -> do not modify code or run unrelated verification.

Use existing automated tests when they are present and relevant.

Do not create a new test project, test framework, fixture infrastructure, Testcontainers setup, or end-to-end environment merely to satisfy this document.

Testing infrastructure is expected to grow over time. When new verification capabilities are introduced, prefer using them automatically for relevant changes.

## 2. Fast Inner Loop

During implementation, use the smallest available feedback loop that can detect mistakes quickly.

Typical current loop:

```text
implement
  -> build affected project/module
  -> analyzer/compiler/configuration failure?
       yes -> diagnose -> smallest corrective change -> rerun
       no  -> continue
```

When relevant automated tests already exist:

```text
implement
  -> targeted build/test
  -> failure?
       yes -> diagnose -> smallest corrective change -> rerun
       no  -> continue
```

Do not repeatedly build the entire solution when an affected project or module provides sufficient feedback.

Do not repeatedly run a full regression suite while diagnosing a localized failure when a targeted check is available.

## 3. Verification Capability Discovery

Before running verification, determine what the repository currently supports.

Check for relevant existing capabilities such as:

```text
solution/project build
dotnet format
build-time analyzers
migration tooling
architecture tests
unit tests
integration tests
API/end-to-end tests
scripts/verify.sh
CI validation
```

Only rely on capabilities that actually exist.

If a verification mechanism is documented but not yet implemented, treat it as a future target rather than a required current step.

Do not report missing future infrastructure as a defect in the feature being implemented.

## 4. Full Verification

Run full repository verification when:

- the user explicitly requests full verification, regression verification, or equivalent; or
- an accepted execution plan identifies repository-wide verification as necessary for a high-risk or cross-cutting change.

If the repository contains:

```bash
./scripts/verify.sh
```

use it as the repository-owned full verification entry point.

Do not assume this script exists. Verify it before invoking it.

The script should execute only verification capabilities currently supported by the repository.

For example, the current workflow may include:

1. restore;
2. build;
3. analyzer/compiler validation;
4. `dotnet format --verify-no-changes` when configured;
5. other repository-owned checks that currently exist.

As the testing infrastructure matures, the full workflow may later include:

1. unit tests;
2. architecture tests;
3. integration tests;
4. end-to-end/API tests.

Those future test stages become required only after the corresponding infrastructure actually exists and is adopted by the repository.

## 5. Failure Handling

If a verification stage fails:

1. determine whether the failure was caused by the current change;
2. diagnose the root cause;
3. make the smallest corrective change;
4. rerun the smallest relevant failing check;
5. repeat until the affected verification is green;
6. rerun broader verification if it was required for the task.

Do not:

- suppress compiler or analyzer errors without understanding them;
- disable valid repository checks;
- weaken an existing test merely to make it pass;
- change expected behavior merely to hide a defect;
- introduce unrelated infrastructure changes to bypass verification;
- claim a verification step was successful when it was not executed.

If verification cannot complete because of:

- an environment limitation;
- missing infrastructure;
- unavailable external dependencies;
- a pre-existing repository failure;

report exactly what was verified, what was not verified, and why.

## 6. Future Testing Strategy

Automated testing is an intended future capability of the repository.

When test infrastructure is introduced, verification should progressively evolve toward:

```text
application/domain behavior
    -> unit tests

persistence/repository/migrations
    -> integration tests

HTTP/API behavior
    -> API/end-to-end tests

dependency boundaries
    -> architecture tests
```

Do not introduce all testing layers merely because they are listed here.

Testing capabilities should be added intentionally as part of dedicated work or an accepted implementation plan.

Once a testing layer exists, update this document and the repository verification script so future agents