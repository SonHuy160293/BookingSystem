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

    private PermissionDefinition() { }

    private PermissionDefinition(
       string function,
       string action,
       string name,
       string value,
       string groupName,
       string? description,
       int displayOrder)
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
    }

    public static PermissionDefinition Create(
          string function,
          string action,
          string name,
          string value,
          string groupName,
          string? description,
          int displayOrder)
          => new(function, action, name, value, groupName, description, displayOrder);

    public void Sync(
        string function,
        string action,
        string name,
        string groupName,
        string? description,
        int displayOrder)
    {
        Function = function;
        Action = action;
        Name = name;
        GroupName = groupName;
        Description = description;
        DisplayOrder = displayOrder;
        IsEnabled = true;
    }

    public void Disable()
        => IsEnabled = false;
}
