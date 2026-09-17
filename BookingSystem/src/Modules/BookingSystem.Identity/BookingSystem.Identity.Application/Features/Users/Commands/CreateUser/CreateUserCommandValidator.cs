using BookingSystem.SharedKernel.Abstractions.Message;
using System.Text.RegularExpressions;

namespace BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private static readonly Regex EmailRegex = new(
        "^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$",
        RegexOptions.CultureInvariant);

    public Task ValidateAsync(CreateUserCommand createUserCommand, CancellationToken cancellationToken)
    {

        var errors = new List<string>();

        var request = createUserCommand.CreateUserRequest;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }

        if (!EmailRegex.IsMatch(request.Email))
        {
            errors.Add("Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }

        if (request.Password.Count(char.IsDigit) < 6)
        {
            errors.Add("Password must contain at least 6 digits.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
