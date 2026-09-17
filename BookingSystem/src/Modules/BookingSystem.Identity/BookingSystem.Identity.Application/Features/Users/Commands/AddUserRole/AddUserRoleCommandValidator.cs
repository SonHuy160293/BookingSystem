using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Users.Commands.AddUserRole;

public sealed class AddUserRoleCommandValidator : IValidator<AddUserRoleCommand>
{
    public Task ValidateAsync(AddUserRoleCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();

        if (command.UserId == Guid.Empty)
        {
            errors.Add("UserId must not be empty.");
        }

        if (command.Request.RoleId == Guid.Empty)
        {
            errors.Add("RoleId must not be empty.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
