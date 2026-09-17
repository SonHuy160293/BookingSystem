using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Roles.Commands.AddRolePermission;

public sealed class AddRolePermissionCommandValidator : IValidator<AddRolePermissionCommand>
{
    public Task ValidateAsync(AddRolePermissionCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var permissions = command.Request?.Permissions;
        var errors = new List<string>();
        if (permissions is null || permissions.Count == 0)
        {
            errors.Add("Permissions must contain at least one value.");
        }
        else if (permissions.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Permissions must not contain blank values.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
