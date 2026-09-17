using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.SharedKernel.Abstractions.Message;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
