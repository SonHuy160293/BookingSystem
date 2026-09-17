# Tests

Root `AGENTS.md` applies here.

Architecture tests should check project dependency direction without referencing AppHost or business implementation assemblies. Add behavioral tests near the relevant module when changing runtime behavior. Do not weaken an assertion to hide a dependency violation.
