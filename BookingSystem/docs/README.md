# Repository Knowledge Base

The `docs/` tree is the versioned system of record for engineering knowledge that is too detailed for root `AGENTS.md`.

Root `AGENTS.md` should remain a small entry point: repository map, global invariants, work protocol, verification contract, and links into this knowledge base.

## Knowledge map

| Topic | Source of truth |
| --- | --- |
| Repository-level architecture | `ARCHITECTURE.md` |
| Runtime orchestration / Aspire / observability boundaries | `docs/architecture/runtime-orchestration.md` |
| Repository investigation / CodeGraph | `docs/development/repository-investigation.md` |
| Verification | `docs/development/verification.md` |
| Typed internal/external HTTP clients, resilience and correlation | [HTTP clients](development/http-clients.md) |
| Cinema/Film management APIs and Identity integration | `docs/development/cinema-management-api.md` |
| Booking inventory, payment and shared reliability persistence | [Booking reliability](development/booking-reliability.md) |
| Cinema seat catalog, layout assignments and publication rules | [Cinema seating](development/cinema-seating.md) |
| Identity login, refresh tokens and device sessions | `docs/development/identity-auth-sessions.md` |
| Identity hosted cleanup and Worker library | [Worker README](../src/Modules/BookingSystem.Identity/BookingSystem.Identity.Worker/README.md) |
| YARP gateway routes, security and operations | `docs/development/gateway.md` |
| Deployment/environment handling | `docs/development/deployment.md` |
| Planning and execution-plan lifecycle | `docs/PLANS.md` |
| Active long-running plans | `docs/exec-plans/active/` |
| Completed execution history | `docs/exec-plans/completed/` |
| Durable architecture decisions | `docs/decisions/` |
| Quality principles / drift-control targets | `docs/QUALITY.md` |
| External/sample/reference material | `docs/references/` when present |

## Documentation rules

- Keep one clear source of truth for a rule. Link to it instead of copying large blocks between files.
- Prefer explanations of **why** in architecture/decision documents; keep root instructions terse.
- When source code and documentation disagree, verify runtime/source behavior before deciding which is stale.
- Update affected documentation in the same change when a durable architecture, workflow, or operational contract changes.
- Do not create detailed documentation for empty scaffolds before real responsibilities exist.
- Reference material is evidence, not automatically current implementation truth.

## Agent-legibility rule

A durable decision that exists only in chat, a developer's memory, or an external discussion is not reliable repository context for future agents. If it materially affects future implementation choices, capture it in the appropriate versioned repository document.
