using BookingSystem.SharedKernel.Abstractions.Domain;

namespace BookingSystem.Identity.Domain.Models;

public sealed class User : Entity<Guid>
{
    public string? FullName { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Configuration { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? UserName { get; private set; }
    public string? NormalizedUserName { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? SecurityStamp { get; private set; }
    public string? ConcurrencyStamp { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public bool LockoutEnabled { get; private set; }
    public int AccessFailedCount { get; private set; }
    public string? AvatarUrl { get; private set; }

    private User() { }

    private User(Guid id, bool isEnabled, string fullName, string userName, string email, string jobTitle)
        : base(id)
    {
        IsEnabled = isEnabled;
        FullName = fullName;
        UserName = userName;
        Email = email;
        JobTitle = jobTitle;
    }

    public static User Create(bool isEnabled, string fullName, string userName, string email, string jobTitle)
    {
        return new User(Guid.CreateVersion7(), isEnabled, fullName.Trim(), userName.Trim(), email.Trim(), jobTitle.Trim());
    }

    public void SetIdentityCredentials(
        string normalizedUserName,
        string normalizedEmail,
        string passwordHash,
        string securityStamp,
        string concurrencyStamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityStamp);
        ArgumentException.ThrowIfNullOrWhiteSpace(concurrencyStamp);

        NormalizedUserName = normalizedUserName;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        SecurityStamp = securityStamp;
        ConcurrencyStamp = concurrencyStamp;
    }
}
