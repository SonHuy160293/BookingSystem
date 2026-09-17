# Shared validation guards

## Outcome and scope

Extract reusable validation primitives into `BookingSystem.SharedKernel.Validation.Guard`.
Cinema, Order, Payment and reliability persistence retain their existing helper
entry points and domain-specific rules. Preserve accepted values, normalization,
exception types, property names and messages. No persistence mappings change.

## Implementation

- [x] Add shared condition/exception, GUID, bounded text, SHA-256 and enum guards.
- [x] Delegate module and reliability helpers to the shared primitives.
- [x] Document ownership and review the changed source.

## Decisions

Money, ownership, seat rules, safe-code alphabets, lease rules and JSON validation
stay with their owning module or technical capability. Payment's control-character
policy and Cinema's distinct required/length messages remain unchanged. SharedKernel
gains no dependencies on EF Core, HTTP or module projects.

## Verification

Source/diff review only, per the user's ongoing no-verification-loop instruction.
No restore, build, tests, formatter, Docker execution, seeds or migrations.

## Risks and follow-up

Call sites keep their existing helper signatures to limit behavior changes.
Compilation and executable behavior remain unverified until a separate validation task.

## Completion

Source review covered helper delegation, validation messages, normalization,
Payment's control-character rejection, and existing SharedKernel project references.
No persistence configuration or dependency changes were required. No executable
verification, seeds or migrations were created or run.
