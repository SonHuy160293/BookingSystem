using BookingSystem.Identity.Application.Contracts.Permissions;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Permissions.Queries.GetPermissions;

public sealed record GetPermissionsQuery(GetPermissionsRequest Request) : IQuery<PagedResult<PermissionDto>>;
