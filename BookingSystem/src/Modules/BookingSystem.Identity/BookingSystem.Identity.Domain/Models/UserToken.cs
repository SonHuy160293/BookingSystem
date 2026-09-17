namespace BookingSystem.Identity.Domain.Models;

public sealed class UserToken
{
    private UserToken() { }

    private UserToken(Guid userId, string loginProvider, string name, string? value)
    {
        UserId = userId;
        LoginProvider = loginProvider;
        Name = name;
        Value = value;
    }

    public Guid UserId { get; private set; }
    public string LoginProvider { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Value { get; private set; }

    public static UserToken Create(Guid userId, string loginProvider, string name, string? value)
    {
        ValidateRequired(loginProvider, nameof(loginProvider));
        ValidateRequired(name, nameof(name));
        return new UserToken(userId, loginProvider, name, value);
    }

    private static void ValidateRequired(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is required.");
        }
    }
}
