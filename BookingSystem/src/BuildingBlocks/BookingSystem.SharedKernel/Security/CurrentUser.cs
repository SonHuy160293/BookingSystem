namespace BookingSystem.SharedKernel.Security;

public sealed record CurrentUser(
    string? Id,
    string? UserName = null,
    string? Email = null,
    IReadOnlyCollection<string>? Permissions = null);
