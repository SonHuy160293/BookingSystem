namespace BookingSystem.SharedKernel.Security;

public interface ICurrentUserProvider
{
    CurrentUser? GetCurrentUser();
}
