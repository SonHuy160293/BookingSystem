using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Roles.Commands.AddRolePermission;

public sealed record AddRolePermissionCommand(Guid Id, PermissionRequest Request) : ICommand;
