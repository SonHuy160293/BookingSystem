# AppHost rules

Root `AGENTS.md` applies here.

Define local resource topology and startup dependencies only. Keep resource names stable. Wire a database only to its actual consumers, wait for SQL health before migration, and wait for successful migration completion before API startup. Do not use sleeps or manual port polling.

Do not put business logic, EF entities, repositories, CQRS handlers, or production credentials in AppHost. Use parameters, environment variables, or user secrets for local secrets.
