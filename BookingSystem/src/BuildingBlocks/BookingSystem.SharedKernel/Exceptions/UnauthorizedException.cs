using BookingSystem.SharedKernel.Abstractions.Exceptions;

namespace BookingSystem.SharedKernel.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException()
        : this("Authentication is required to access this resource.") { }

    public UnauthorizedException(string message)
        : base("Unauthorized", message) { }
}
