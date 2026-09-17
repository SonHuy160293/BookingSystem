using System.Net.Mail;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    public Task ValidateAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = command.Request;
        var errors = new List<string>();

        ValidateRequiredLength(request.FullName, nameof(request.FullName), 200, errors);
        ValidateRequiredLength(request.UserName, nameof(request.UserName), 256, errors);
        ValidateRequiredLength(request.JobTitle, nameof(request.JobTitle), 100, errors);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }
        else
        {
            var email = request.Email.Trim();
            if (email.Length > 256)
            {
                errors.Add("Email must not exceed 256 characters.");
            }
            else if (!MailAddress.TryCreate(email, out var parsedEmail)
                     || !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Email format is invalid.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }

    private static void ValidateRequiredLength(
        string value,
        string propertyName,
        int maximumLength,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} is required.");
            return;
        }

        if (value.Trim().Length > maximumLength)
        {
            errors.Add($"{propertyName} must not exceed {maximumLength} characters.");
        }
    }
}
