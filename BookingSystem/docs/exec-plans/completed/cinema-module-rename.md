# Cinema module rename

Rename the existing module to `BookingSystem.Cinema` throughout the solution, preserving unrelated working-tree changes.

Scope includes the five project folders and project files, namespaces, DbContext, project and solution references, Aspire definitions, database configuration, log names, and module documentation. No database is modified; configured database names now use `CinemaDb`.

Acceptance requires all source references and paths to use Cinema, valid project references, successful affected builds, and passing existing architecture tests. Follow [verification guidance](../../development/verification.md).

Progress:
- Source, configuration, documentation, and project names updated.
- Source moved into the Cinema module folder after Windows blocked renaming the parent directory. The old folder was verified to contain only generated caches and removed.
- AppHost and all referenced module projects build successfully (zero errors; existing Aspire/comment-formatting warnings). The initial sandboxed build could not write generated files in moved directories; the build succeeded outside the sandbox.
- All three existing architecture tests pass.
- All solution paths and affected project references resolve. No previous module names remain in source/configuration/documentation when generated output and IDE caches are excluded. `git diff --check` passes.
- The open IDE recreated old generated caches after cleanup. Cleaned them again; reload the solution to stop regeneration from stale IDE state.

Rollback consists of reversing the mechanical rename; preserve existing user edits when doing so. Existing database data is not migrated by this change.
