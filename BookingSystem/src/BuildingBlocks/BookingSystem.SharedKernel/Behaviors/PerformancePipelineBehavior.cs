using System.Diagnostics;
using BookingSystem.SharedKernel.Abstractions.Message;
using Microsoft.Extensions.Logging;

namespace BookingSystem.SharedKernel.Behaviors;

public sealed class PerformancePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TRequest> _logger;

    public PerformancePipelineBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 5000)
        {
            _logger.LogWarning(
                "Long running request: {RequestName} took {ElapsedMilliseconds} ms. {@Request}",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                request);
        }

        return response;
    }
}
