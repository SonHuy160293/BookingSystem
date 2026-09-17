using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(CreateRoleRequest Request) : ICommand<RoleDto>;
