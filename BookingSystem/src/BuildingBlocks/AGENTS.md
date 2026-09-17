# AGENTS.md — BuildingBlocks

Additional instructions for Codex working under `src/BuildingBlocks/`.

Root `AGENTS.md` rules continue to apply.

## Purpose

`BuildingBlocks` contains reusable **technical primitives and infrastructure** shared across modules.

```text
BookingSystem.SharedKernel
    -> CQRS abstractions and mediator
    -> Result / Error
    -> domain-safe primitives
    -> pipeline behaviors
    -> IUnitOfWork
    -> framework-neutral repository contracts
    -> common exceptions

BookingSystem.AspNetCore
    -> middleware
    -> filters
    -> API controller base
    -> ASP.NET Core DI/configuration

BookingSystem.EntityFrameworkCore
    -> reusable EF Core infrastructure
    -> repository base
    -> SaveChanges interceptors

BookingSystem.ServiceDefaults
    -> host-level health, service discovery, and HTTP resilience
    -> delegates telemetry registration to Observability

BookingSystem.Observability
    -> reusable host-only OpenTelemetry resources, export, instrumentation, and privacy filters
```

Do not place module-specific business logic here.

BuildingBlocks must never reference business modules.

Dependencies between BuildingBlocks projects must remain one-way and cycle-free.

## SharedKernel

Keep `SharedKernel` framework-light.

Do not introduce ASP.NET Core or EF Core concerns when they belong in the corresponding BuildingBlock.

Although Domain may reference BuildingBlocks, Domain may use only **domain-safe primitives**.

Domain must not depend on application-oriented concepts such as:

* `ICommand` / `IQuery`;
* handlers;
* validators;
* `IUnitOfWork`;
* repository contracts;
* mediator implementation;
* pipeline behaviors;
* paging/API response models.

## AspNetCore

Keep reusable HTTP/API infrastructure here.

Do not add:

* module-specific controllers;
* module-specific business rules;
* module-specific request/response models.

HTTP concerns should not leak into `SharedKernel`.

## EntityFrameworkCore

Keep reusable EF Core infrastructure here.

Do not add:

* module DbContexts;
* module entities;
* module repositories;
* migrations;
* seed data;
* module-specific persistence logic.

Those belong to the owning module's Infrastructure project.

## ServiceDefaults

Keep this project infrastructure-only. It must not depend on modules or contain domain models, CQRS messages, repositories, DTOs, `Result<T>`, or business logic. Only executable hosts reference it.

## Observability

Only executable hosts and ServiceDefaults reference this project. Keep SDK registration, export options, resources, instrumentation, and privacy filters here; keep CQRS activities and meters in SharedKernel using framework-neutral .NET diagnostics. Register one OTLP logging provider, with Serilog forwarding to it. Do not add a parallel centralized log sink or make application readiness depend on telemetry availability.

## Adding shared abstractions

Before adding something to BuildingBlocks, verify that:

1. it is technical rather than business-specific;
2. it is genuinely reusable or repository-wide;
3. an equivalent does not already exist;
4. it preserves dependency direction.

Do not create abstractions for hypothetical future reuse.

Prefer keeping code inside a module until reuse becomes real.

Repository interfaces that describe framework-neutral application persistence ports belong in
`BookingSystem.SharedKernel.Abstractions.Persistence`. EF Core implementations and module-specific
repository behavior remain in the appropriate Infrastructure project.

## Shared changes

Changes to existing BuildingBlocks can have broad impact.

Before changing a shared contract such as `ISender`, `ICommand`, `IQuery`, `IUnitOfWork`, `Result`, pipeline behaviors, repository bases, middleware, or interceptors, inspect its consumers and blast radius.

Prefer backward-compatible changes when practical.
