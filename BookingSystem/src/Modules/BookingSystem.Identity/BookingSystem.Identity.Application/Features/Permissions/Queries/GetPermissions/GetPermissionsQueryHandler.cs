using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Permissions;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Permissions.Queries.GetPermissions;

public sealed class GetPermissionsQueryHandler : IQueryHandler<GetPermissionsQuery, PagedResult<PermissionDto>>
{
    private readonly IPermissionRepository _repository;

    public GetPermissionsQueryHandler(IPermissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<PermissionDto>>> HandleAsync(GetPermissionsQuery query, CancellationToken cancellationToken)
    {
        var permissions = await _repository.ListAsync(query.Request, cancellationToken);
        return Result.Success(permissions);
    }
}
