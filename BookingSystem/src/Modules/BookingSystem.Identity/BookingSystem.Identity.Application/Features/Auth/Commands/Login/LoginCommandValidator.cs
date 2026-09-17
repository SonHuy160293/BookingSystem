using System.Net.Mail;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : IValidator<LoginCommand>
{
    public Task ValidateAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = command.Request;
        var errors = new List<string>();
        if (request is null)
        {
            throw new CqrsValidationException(["Login request is required."]);
        }

        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 256 || !MailAddress.TryCreate(request.Email.Trim(), out var email) || !string.Equals(email.Address, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("A valid email of at most 256 characters is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }

        if (request.ClientId is not null && (string.IsNullOrWhiteSpace(request.ClientId) || request.ClientId.Length > 128))
        {
            errors.Add("Client ID must be between one and 128 characters.");
        }

        if (request.DeviceName?.Length > 128)
        {
            errors.Add("Device name must be at most 128 characters.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
