using BookingSystem.SharedKernel.Abstractions.Exceptions;

namespace BookingSystem.SharedKernel.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string code, string message)
        : base(code, message) { }

    public ConflictException(string message)
        : this("Conflict", message) { }
}
