using BookingSystem.SharedKernel.Abstractions.Exceptions;

namespace BookingSystem.SharedKernel.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException()
        : this("Access denied.") { }

    public ForbiddenException(string message)
        : base("Forbidden", message) { }
}
