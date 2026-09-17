using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, PagedResult<RoleDto>>
{
    private readonly IRolesRepository _repository;

    public GetRolesQueryHandler(IRolesRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<RoleDto>>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await _repository.ListAsync(query.Request, cancellationToken);
        return Result.Success(roles);
    }
}
