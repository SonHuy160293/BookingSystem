using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.SharedKernel.Cqrs;

public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
