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

        if (command.Request.TenantId == Guid.Empty)
        {
            errors.Add("TenantId must not be empty when supplied.");
        }

        if (command.Request.BranchId == Guid.Empty)
        {
            errors.Add("BranchId must not be empty when supplied.");
        }

        if (command.Request.BranchId.HasValue && !command.Request.TenantId.HasValue)
        {
            errors.Add("TenantId is required when BranchId is supplied.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
