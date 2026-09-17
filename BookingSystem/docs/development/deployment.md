# Deployment and Environment Guidance

## Docker Compose

`deploy/compose/` holds the local Docker Compose stacks, including:

- `docker-compose.yaml`;
- `docker-compose.override.yaml`;
- `docker-compose.infrastructure.yaml`.

`deploy/compose/` remains a supported deployment/local-run path unless an explicit decision changes that contract.

## Environment configuration

`deploy/env/` holds environment configuration.

Treat `.prod.env` as read-only unless the user explicitly asks to change it.

Do not modify production configuration as a side effect of an unrelated feature task.

If `.prod.env` contains real credentials or secrets, it must not be committed.

## References

`docs/references/` contains sample/reference material when present. It is not part of the build and must not be treated as current implementation without verification against source.

## Scope discipline

Deployment changes should be intentional. Adding a new runtime dependency, changing migration ownership, or replacing a supported deployment path is an architecture/operations decision, not incidental feature work.
