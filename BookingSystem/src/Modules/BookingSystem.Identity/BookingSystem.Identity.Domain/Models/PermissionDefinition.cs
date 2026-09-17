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

    private PermissionDefinition(Guid id, string function, string action, string name, string value, string groupName, string? description, int displayOrder, bool isEnabled)
    {
        Id = id;
        Function = function;
        Action = action;
        Name = name;
        Value = value;
        GroupName = groupName;
        Description = description;
        DisplayOrder = displayOrder;
        IsEnabled = isEnabled;
    }

    public static PermissionDefinition Create(string function, string action, string name, string value, string groupName, string? description, int displayOrder, bool isEnabled)
    {
        ValidateRequired(function, nameof(function));
        ValidateRequired(action, nameof(action));
        ValidateRequired(name, nameof(name));
        ValidateRequired(value, nameof(value));
        ValidateRequired(groupName, nameof(groupName));
        return new PermissionDefinition(Guid.CreateVersion7(), function, action, name, value, groupName, description, displayOrder, isEnabled);
    }

    private static void ValidateRequired(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is required.");
        }   
    }
}
