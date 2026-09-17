using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenants;

public sealed class GetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, PagedResult<TenantDto>>
{
    private readonly ITenantRepository _repository;

    public GetTenantsQueryHandler(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<TenantDto>>> HandleAsync(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await _repository.ListAsync(query.Request, cancellationToken);
        return Result.Success(tenants);
    }
}
