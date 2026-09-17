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

## Adding shared abstractions

Before adding something to BuildingBlocks, verify that:

1. it is technical rather than business-specific;
2. it is genuinely reusable or repository-wide;
3. an equivalent does not already exist;
4. it preserves dependency direction.

Do not create abstractions for hypothetical future reuse.

Prefer keeping code inside a module until reuse becomes real.

## Shared changes

Changes to existing BuildingBlocks can have broad impact.

Before changing a shared contract such as `ISender`, `ICommand`, `IQuery`, `IUnitOfWork`, `Result`, pipeline behaviors, repository bases, middleware, or interceptors, inspect its consumers and blast radius.

Prefer backward-compatible changes when practical.
