# AGENTS.md — Identity Worker

Root, Modules, and Identity module instructions apply.

This project is a .NET class library containing Identity background services.
The Identity API hosts these services through `AddIdentityWorker()`; Worker is
not an executable and has no independent deployment or application settings file.

## Boundaries

- Reference Identity.Application and SharedKernel, plus the required hosting/DI
  abstractions. Do not reference Infrastructure, API, AppHost, ServiceDefaults,
  or Observability.
- Keep scheduling and orchestration here. Invoke Application commands/queries or
  Application-facing abstractions; business rules and transactions stay there.
- Do not access DbContext, issue SQL, apply migrations, or call SaveChanges here.
- The API composition root supplies Application and Infrastructure registrations,
  validated options, logging, and TimeProvider.

## Hosted-service lifetime

- Register services through the Worker's dependency-injection extension.
- Hosted services are singletons. Resolve scoped repositories and ISender within
  an explicit asynchronous DI scope; never retain them on the singleton.
- Pass the stopping token to waits, queries, and commands. Dispose scopes and
  timers, and treat requested cancellation as normal shutdown.
- Preserve cleanup retention, batching, and distributed locking implemented by
  Application/Infrastructure. Do not replace database coordination with a local
  lock or duplicate persistence rules in Worker.
- Log operational outcomes without tokens, passwords, credential hashes, or
  exception text that could contain authentication parameters.

See [README.md](README.md) for registration, scheduling, configuration, and checks.
