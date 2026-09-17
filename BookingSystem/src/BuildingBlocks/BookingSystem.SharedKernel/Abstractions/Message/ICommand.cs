using BookingSystem.SharedKernel.Abstractions.Shared;

namespace BookingSystem.SharedKernel.Abstractions.Message;

public interface ICommand : IRequest<Result> { }

public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
