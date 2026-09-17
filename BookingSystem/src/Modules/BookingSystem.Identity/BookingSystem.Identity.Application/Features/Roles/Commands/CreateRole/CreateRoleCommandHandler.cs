using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Exceptions;

namespace BookingSystem.Identity.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRolesRepository _repository;

    public CreateRoleCommandHandler(IRolesRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RoleDto>> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var name = command.Request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();

        if (await _repository.NormalizedNameExistsAsync(normalizedName, cancellationToken))
        {
            throw new ConflictException("Role.NameAlreadyExists", $"Role with name '{name}' already exists.");
        }

        var role = Role.Create(command.Request.Description, name, normalizedName, Guid.NewGuid().ToString());
        await _repository.AddAsync(role, cancellationToken);

        return Result.Success(new RoleDto(role.Id, role.Name, role.Description, role.CreatedAt, []));
    }
}
