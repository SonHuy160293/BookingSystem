using BookingSystem.Identity.Application.Abstractions.Repositories;
using BookingSystem.Identity.Application.Contracts.Tenants;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Features.Tenants.Queries.GetTenantById;

public sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly ITenantRepository _repository;

    public GetTenantByIdQueryHandler(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantDto>> HandleAsync(GetTenantByIdQuery query, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetTenantByIdAsync(query.Id, cancellationToken);
        return Result.Success(tenant);
    }
}
