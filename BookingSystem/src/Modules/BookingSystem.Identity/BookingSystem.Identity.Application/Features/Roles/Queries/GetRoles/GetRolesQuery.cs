using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Roles.Queries.GetRoles;

public sealed record GetRolesQuery(GetRolesRequest Request) : IQuery<PagedResult<RoleDto>>;
