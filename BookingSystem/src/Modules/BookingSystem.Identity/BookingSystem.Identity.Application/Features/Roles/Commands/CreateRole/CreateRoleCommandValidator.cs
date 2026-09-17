using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : IValidator<CreateRoleCommand>
{
    public Task ValidateAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();
        var name = command.Request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }
        else
        {
            if (name.Length > 256)
            {
                errors.Add("Name must not exceed 256 characters.");
            }

            if (name.ToUpperInvariant().Length > 256)
            {
                errors.Add("Normalized name must not exceed 256 characters.");
            }
        }

        if (command.Request.Description?.Length > 250)
        {
            errors.Add("Description must not exceed 250 characters.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
