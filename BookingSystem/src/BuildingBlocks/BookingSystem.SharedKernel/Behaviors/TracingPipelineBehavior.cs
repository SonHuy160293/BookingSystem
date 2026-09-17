using System.Diagnostics;
using BookingSystem.SharedKernel.Abstractions.Message;
using Microsoft.Extensions.Logging;

namespace BookingSystem.SharedKernel.Behaviors;

public sealed class TracingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TRequest> _logger;

    public TracingPipelineBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation(
            "Request details: {RequestName} took {ElapsedMilliseconds} ms. {@Request}",
            typeof(TRequest).Name,
            stopwatch.ElapsedMilliseconds,
            request);

        return response;
    }
}
