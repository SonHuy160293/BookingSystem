using BookingSystem.Identity.Domain.Enums;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Domain.Models;

public sealed class PermissionDefinition
{
    public Guid Id { get; private set; }
    public string Function { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string GroupName { get; private set; } = null!;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsEnabled { get; private set; }
    public RbacScopeType ScopeType { get; private set; }

    private PermissionDefinition() { }

    private PermissionDefinition(
       string function,
       string action,
       string name,
       string value,
       string groupName,
       string? description,
       int displayOrder,
       RbacScopeType scopeType)
    {
        Id = Guid.CreateVersion7();
        Function = function;
        Action = action;
        Name = name;
        Value = value;
        GroupName = groupName;
        Description = description;
        DisplayOrder = displayOrder;
        IsEnabled = true;
        ScopeType = scopeType;
    }

    public static PermissionDefinition Create(
          string function,
          string action,
          string name,
          string value,
          string groupName,
          string? description,
          int displayOrder,
          RbacScopeType scopeType = RbacScopeType.Platform)
    {
        ValidateScopeType(scopeType);
        return new PermissionDefinition(function, action, name, value, groupName, description, displayOrder, scopeType);
    }

    public void Sync(
        string function,
        string action,
        string name,
        string groupName,
        string? description,
        int displayOrder,
        RbacScopeType? scopeType = null)
    {
        if (scopeType is { } specifiedScopeType)
        {
            ValidateScopeType(specifiedScopeType);
        }

        Function = function;
        Action = action;
        Name = name;
        GroupName = groupName;
        Description = description;
        DisplayOrder = displayOrder;
        IsEnabled = true;
        ScopeType = scopeType ?? ScopeType;
    }

    public void Disable()
        => IsEnabled = false;

    private static void ValidateScopeType(RbacScopeType scopeType)
    {
        if (!Enum.IsDefined(scopeType))
        {
            throw new ValidationException(
                [new ValidationError(nameof(ScopeType), "ScopeType must be PLATFORM, TENANT, or BRANCH.")]);
        }
    }
}
