# Planning and Execution Plans

Plans are first-class engineering artifacts when they carry information future agent runs need in order to continue work safely.

## 1. Match plan weight to task weight

### Small, explicit, low-risk task

Use an ephemeral lightweight plan in the active agent run. Do not create repository files only to document trivial steps.

### Complex or long-running task

Create a checked-in execution plan when the work is one or more of the following:

- cross-cutting across multiple projects/modules;
- architectural;
- migration-heavy or operationally risky;
- likely to span multiple agent runs/PRs;
- expected to accumulate decisions, discoveries, or follow-up work;
- difficult for a future agent to reconstruct from the final diff alone.

Place active plans under `docs/exec-plans/active/`. Move completed plans to `docs/exec-plans/completed/` when the work is finished.

## 2. Plan contents

A useful execution plan should include:

- problem / desired outcome;
- scope and non-goals;
- relevant repository context and source-of-truth links;
- assumptions;
- acceptance criteria;
- implementation slices or milestones;
- verification strategy;
- risks and rollback considerations when relevant;
- progress log for long-running work;
- decision log for choices made during execution;
- unresolved items / follow-ups.

Do not turn a plan into speculative implementation detail when the repository has not yet been investigated.

## 3. Human judgment boundary

Ask the user to approve or adjust the plan before implementation when the work requires material human judgment, such as:

- ambiguous product intent;
- destructive behavior;
- security-sensitive trade-offs;
- materially different architecture choices with different long-term consequences.

Do not require approval merely because a change is large if the accepted requirement and repository architecture determine one clear implementation path.

## 4. Durable decisions versus execution details

Execution plans explain **how a change is being carried out**.

Architecture decision records under `docs/decisions/` explain **why a durable choice exists**.

If a plan makes a decision future agents must preserve after the plan is completed, extract that decision into `docs/decisions/` or the appropriate architecture document.
