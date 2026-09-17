using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Roles;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRolesRepository _repository;

    public GetRoleByIdQueryHandler(IRolesRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RoleDto>> HandleAsync(GetRoleByIdQuery query, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByIdAsync(query.Id, cancellationToken);
        return Result.Success(role);
    }
}
