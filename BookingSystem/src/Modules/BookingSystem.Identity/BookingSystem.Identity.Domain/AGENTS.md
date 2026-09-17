# BookingSystem.Identity.Domain Instructions

This file defines the conventions that must be followed when creating or modifying code inside the `BookingSystem.Identity.Domain` project.

The Domain layer contains business entities and domain behavior.

Follow existing Domain conventions before introducing new patterns.

---

# Architecture

The Domain project must remain independent from infrastructure and persistence concerns.

The Domain project may reference:

* `BookingSystem.SharedKernel`
* other approved domain abstractions

The Domain project must not reference:

* Entity Framework Core
* ASP.NET Core
* Infrastructure projects
* API projects
* database-specific libraries
* external service implementations

Do not add persistence configuration to domain entities.

---

# Entity Base Types

Entities should inherit from the appropriate abstraction from:

```csharp
BookingSystem.SharedKernel.Abstractions.Domain
```

For entities supporting soft deletion, use:

```csharp
SoftDeletableEntity<Guid>
```

Example:

```csharp
public sealed class User : SoftDeletableEntity<Guid>
{
}
```

Do not redeclare properties already provided by the base entity.

Examples may include:

* `Id`
* creation metadata
* update metadata
* deletion metadata
* soft-delete state

Always inspect the base class before adding duplicate properties.

---

# Entity IDs

Use `Guid` as the entity identifier unless an existing model explicitly requires another type.

New aggregate/entity IDs should use UUID version 7:

```csharp
Guid.CreateVersion7()
```

Do not use:

```csharp
Guid.NewGuid()
```

for newly created domain entities unless an existing project convention explicitly requires it.

ID generation should normally happen inside the entity factory method.

Example:

```csharp
return new User(
    Guid.CreateVersion7(),
    name,
    email,
    passwordHash);
```

---

# Entity Declaration

Domain entities should normally be declared as `sealed`.

Example:

```csharp
public sealed class User : SoftDeletableEntity<Guid>
{
}
```

Do not make an entity inheritable unless inheritance is explicitly required by the domain model.

---

# Property Encapsulation

Entity state must not be freely mutable from outside the entity.

Prefer:

```csharp
public string Name { get; private set; }
```

Do not use:

```csharp
public string Name { get; set; }
```

unless there is a specific reason documented by the existing model.

State changes should happen through:

* factory methods
* domain methods
* explicitly defined behavior

rather than public property setters.

---

# Constructors

Persistent entities should provide a private parameterless constructor when required by the persistence mechanism.

Example:

```csharp
private User()
{
}
```

Do not expose this constructor publicly.

The constructor used to initialize valid entity state should also normally be private.

Example:

```csharp
private User(
    Guid id,
    string name,
    string email,
    string passwordHash)
    : base(id)
{
    Name = name;
    Email = email;
    PasswordHash = passwordHash;
}
```

Objects should normally be created through factory methods instead of constructors.

---

# Factory Methods

Use static factory methods for entity creation.

Preferred naming:

```csharp
Create(...)
```

Example:

```csharp
public static User Create(
    string name,
    string email,
    string passwordHash)
{
    // validation

    return new User(
        Guid.CreateVersion7(),
        name,
        email,
        passwordHash);
}
```

Factory methods are responsible for ensuring a new entity starts in a valid domain state.

Do not create domain entities using object initializers such as:

```csharp
var user = new User
{
    Name = name
};
```

---

# Domain Validation

Validate invariants before constructing or mutating an entity.

For required strings, reject:

* `null`
* empty strings
* whitespace-only strings

Example:

```csharp
if (string.IsNullOrWhiteSpace(name))
{
    throw new InvalidOperationException("Name is required.");
}
```

Do not allow an entity to be created in an invalid state.

When adding validation, follow existing exception conventions in the codebase.

If the project later introduces a dedicated domain exception such as:

```csharp
DomainException
```

prefer that convention consistently instead of introducing multiple unrelated exception types.

---

# Entity State Changes

Do not expose public setters solely to make updating entities easier.

Instead, introduce domain methods representing meaningful behavior.

Prefer:

```csharp
public void Rename(string name)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        throw new InvalidOperationException("Name is required.");
    }

    Name = name;
}
```

instead of:

```csharp
user.Name = name;
```

Methods should represent domain intent whenever practical.

Examples:

```csharp
user.Rename(name);
user.ChangeEmail(email);
user.Deactivate();
```

Do not add generic `Update(...)` methods automatically if more explicit domain behavior is appropriate.

---

# Nullability

Use nullable reference types correctly.

A required property should normally be non-nullable:

```csharp
public string Name { get; private set; }
```

An optional property should explicitly use `?`:

```csharp
public string? DisplayName { get; private set; }
```

Do not mark a property nullable merely to satisfy the compiler if the domain requires it.

Required values should be initialized through constructors or factory methods.

---

# Collection Navigation Properties

When an entity owns a collection, prevent external replacement of the collection.

Prefer a private backing collection when domain behavior needs to control modifications.

Example:

```csharp
private readonly List<UserRole> _roles = [];

public IReadOnlyCollection<UserRole> Roles => _roles;
```

Modify collections through domain methods:

```csharp
public void AddRole(UserRole role)
{
    // validation

    _roles.Add(role);
}
```

Avoid exposing mutable collections when external code should not freely modify domain state.

Do not automatically introduce collection navigation properties unless they are required by the domain model.

---

# Relationships

Domain entities may contain:

* foreign-key-like identifiers when they are meaningful to the domain
* navigation references when consistent with the existing model
* domain collections

Do not add EF Core attributes.

Do not use:

```csharp
[Key]
[Required]
[MaxLength]
[ForeignKey]
[Table]
```

inside Domain entities.

Relationship configuration belongs in Infrastructure.

---

# Persistence Independence

Do not add any of the following inside Domain entities:

```csharp
IEntityTypeConfiguration<T>
EntityTypeBuilder<T>
DbContext
DbSet<T>
HasKey(...)
HasIndex(...)
HasOne(...)
HasMany(...)
HasColumnType(...)
```

Do not reference EF Core namespaces.

Database mappings belong in the Infrastructure layer.

---

# Passwords and Credentials

Do not store plaintext passwords in domain entities.

Prefer:

```csharp
public string PasswordHash { get; private set; }
```

instead of:

```csharp
public string Password { get; private set; }
```

The Domain entity should receive a password hash or another approved credential representation.

Password hashing itself should normally be handled by an appropriate application/security service unless the architecture explicitly assigns that responsibility elsewhere.

Never log:

* plaintext passwords
* password hashes
* authentication secrets
* tokens

---

# Naming

Use PascalCase for:

* classes
* properties
* methods
* enums

Use camelCase for:

* parameters
* local variables
* private fields where appropriate

Entity names should be singular.

Prefer:

```text
User
Role
Permission
UserRole
```

instead of:

```text
Users
Roles
Permissions
```

Namespaces must follow the existing project structure.

Example:

```csharp
namespace BookingSystem.Identity.Domain.Models;
```

---

# Creating Entities From ERD / DBML

When generating Domain entities from an ERD, DBML, or other database design:

1. Inspect existing entities first.
2. Inspect the relevant base entity classes.
3. Preserve domain relationships.
4. Use the established ID strategy.
5. Use private setters.
6. Add a private parameterless constructor.
7. Add private constructors for initialization.
8. Add static factory methods for creation.
9. Validate required domain state.
10. Keep persistence configuration out of Domain.

Do not blindly convert every database column into a publicly mutable property.

The generated entity should follow the existing domain modeling style, not merely mirror the database schema.

---

# Example Entity

Use this style as the baseline:

```csharp
using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Identity.Domain.Models;

public sealed class User : SoftDeletableEntity<Guid>
{
    public string Name { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    private User()
    {
    }

    private User(
        Guid id,
        string name,
        string email,
        string passwordHash)
        : base(id)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Create(
        string name,
        string email,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new InvalidOperationException("Password hash is required.");
        }

        return new User(
            Guid.CreateVersion7(),
            name,
            email,
            passwordHash);
    }
}
```

---

# Before Modifying Domain Code

Before creating or changing an entity:

1. Inspect similar entities in the same project.
2. Inspect the base class being inherited.
3. Reuse established conventions.
4. Preserve entity encapsulation.
5. Do not introduce infrastructure dependencies.
6. Do not weaken property visibility simply to make persistence easier.
7. Do not add new abstractions unless they solve an actual requirement.
8. Keep changes focused on the requested task.

When existing code conflicts with this document, identify the conflict before introducing a new convention.
