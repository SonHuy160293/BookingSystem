using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.Identity.Application.Abstractions.Persistence;

public interface IReadRepositoryBase<TDto, in TRequest>
    where TRequest : PagedRequest
{
    Task<TDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TDto>> ListAsync(TRequest request, CancellationToken cancellationToken);
}
