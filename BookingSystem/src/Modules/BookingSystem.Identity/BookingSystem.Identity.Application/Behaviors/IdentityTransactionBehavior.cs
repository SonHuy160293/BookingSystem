using BookingSystem.Identity.Application.Abstractions.Persistence;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Persistence;

namespace BookingSystem.Identity.Application.Behaviors;

public sealed class IdentityTransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthUnitOfWork _authUnitOfWork;
    public IdentityTransactionBehavior(IUnitOfWork unitOfWork, IAuthUnitOfWork authUnitOfWork)
    {
        _unitOfWork = unitOfWork;
        _authUnitOfWork = authUnitOfWork;
    }

    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IAuthCommand)
        {
            return _authUnitOfWork.ExecuteAuthTransactionAsync(_ => next(), cancellationToken);
        }

        bool isCommand = typeof(ICommand).IsAssignableFrom(typeof(TRequest)) || typeof(TRequest).GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommand<>));
        return isCommand ? _unitOfWork.ExecuteInTransactionAsync(_ => next(), cancellationToken) : next();
    }
}
