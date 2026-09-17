using BookingSystem.SharedKernel.Abstractions.Exceptions;

namespace BookingSystem.SharedKernel.Exceptions;

public class BadRequestException : DomainException
{
    public BadRequestException(string code, string message)
        : base(code, message) { }

    public BadRequestException(string message)
        : this("BadRequest", message) { }
}
