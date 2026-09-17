namespace BookingSystem.Identity.Infrastructure.Persistence.Core;

public sealed class AuthDatabaseException : Exception
{
    public AuthDatabaseException()
        : base("The authentication transaction could not be completed.")
    {
    }
}
