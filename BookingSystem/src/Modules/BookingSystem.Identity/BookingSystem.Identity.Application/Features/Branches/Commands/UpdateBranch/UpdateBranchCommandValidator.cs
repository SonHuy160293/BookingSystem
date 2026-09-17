using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Branches.Commands.UpdateBranch;

public sealed class UpdateBranchCommandValidator : IValidator<UpdateBranchCommand>
{
    public Task ValidateAsync(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();
        var name = command.Request.Name?.Trim();
        var address = command.Request.Address?.Trim();

        if (command.Id == Guid.Empty)
        {
            errors.Add("Id must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }
        else if (name.Length > 250)
        {
            errors.Add("Name must not exceed 250 characters.");
        }

        if (address is { Length: > 250 })
        {
            errors.Add("Address must not exceed 250 characters.");
        }

        if (errors.Count > 0)
        {
            throw new CqrsValidationException(errors);
        }

        return Task.CompletedTask;
    }
}
