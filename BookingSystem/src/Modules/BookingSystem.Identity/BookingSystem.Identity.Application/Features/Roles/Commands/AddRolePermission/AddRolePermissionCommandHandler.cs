using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;
using BookingSystem.SharedKernel.Security;

namespace BookingSystem.Identity.Application.Features.Roles.Commands.AddRolePermission;

public sealed class AddRolePermissionCommandHandler : ICommandHandler<AddRolePermissionCommand>
{
    private readonly IRolesRepository _repository;

    public AddRolePermissionCommandHandler(IRolesRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> HandleAsync(AddRolePermissionCommand command, CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(command.Id, cancellationToken);
        var requested = command.Request.Permissions.ToHashSet(StringComparer.Ordinal);
        var enabled = await _repository.GetEnabledPermissionValuesAsync(requested, cancellationToken);
        var invalid = requested.Except(enabled, StringComparer.Ordinal).ToArray();
        if (invalid.Length > 0)
        {
            throw new CqrsValidationException(invalid.Select(value => $"Permission '{value}' does not exist or is disabled.").ToArray());
        }

        var assigned = await _repository.GetAssignedPermissionValuesAsync(role.Id, cancellationToken);
        var claims = requested.Except(assigned, StringComparer.Ordinal)
            .Select(value => RoleClaim.Create(role.Id, CustomClaims.Permission, value)).ToArray();
        if (claims.Length > 0)
        {
            await _repository.AddRoleClaimsAsync(claims, cancellationToken);
        }

        return Result.Success();
    }
}
