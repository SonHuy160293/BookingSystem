using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.SharedKernel.Abstractions.Message;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
