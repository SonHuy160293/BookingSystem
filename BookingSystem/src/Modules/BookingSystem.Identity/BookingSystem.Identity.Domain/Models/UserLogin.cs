namespace BookingSystem.Identity.Domain.Models;

public sealed class UserLogin
{
    private UserLogin() { }

    private UserLogin(string loginProvider, string providerKey, string? providerDisplayName, Guid userId)
    {
        LoginProvider = loginProvider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        UserId = userId;
    }

    public string LoginProvider { get; private set; } = null!;
    public string ProviderKey { get; private set; } = null!;
    public string? ProviderDisplayName { get; private set; }
    public Guid UserId { get; private set; }

    public static UserLogin Create(string loginProvider, string providerKey, string? providerDisplayName, Guid userId)
    {
        ValidateRequired(loginProvider, nameof(loginProvider));
        ValidateRequired(providerKey, nameof(providerKey));
        return new UserLogin(loginProvider, providerKey, providerDisplayName, userId);
    }

    private static void ValidateRequired(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is required.");
        }
    }
}
