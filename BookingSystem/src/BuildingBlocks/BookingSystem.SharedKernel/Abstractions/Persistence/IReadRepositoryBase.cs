using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.SharedKernel.Abstractions.Persistence;

public interface IReadRepositoryBase<TDto, TId, in TRequest>
    where TRequest : PagedRequest
{
    Task<TDto> GetByIdAsync(TId id, CancellationToken cancellationToken);

    Task<PagedResult<TDto>> ListAsync(TRequest request, CancellationToken cancellationToken);
}
