using BookingSystem.Identity.Application.Contracts.Users;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Users.Commands.AddUserRole;

public sealed record AddUserRoleCommand(Guid UserId, RoleAssignmentRequest Request) : ICommand;
