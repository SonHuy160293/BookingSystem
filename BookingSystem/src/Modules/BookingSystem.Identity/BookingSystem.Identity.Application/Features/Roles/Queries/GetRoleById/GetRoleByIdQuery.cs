using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.Identity.Application.Features.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(Guid Id) : IQuery<RoleDto>;
