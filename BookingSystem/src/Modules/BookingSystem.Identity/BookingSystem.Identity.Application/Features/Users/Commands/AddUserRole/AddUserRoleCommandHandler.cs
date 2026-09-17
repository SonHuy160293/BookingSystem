using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Domain.Models;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Users.Commands.AddUserRole;

public sealed class AddUserRoleCommandHandler : ICommandHandler<AddUserRoleCommand>
{
    private readonly IRolesRepository _rolesRepository;
    private readonly IUserRepository _userRepository;

    public AddUserRoleCommandHandler(IUserRepository userRepository, IRolesRepository rolesRepository)
    {
        _userRepository = userRepository;
        _rolesRepository = rolesRepository;
    }

    public async Task<Result> HandleAsync(AddUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        var role = await _rolesRepository.GetByIdAsync(command.Request.RoleId, cancellationToken);

        if (await _userRepository.HasRoleAsync(user.Id, role.Id, cancellationToken))
        {
            return Result.Success();
        }

        await _userRepository.AddUserRoleAsync(UserRole.Create(user.Id, role.Id), cancellationToken);

        return Result.Success();
    }
}
