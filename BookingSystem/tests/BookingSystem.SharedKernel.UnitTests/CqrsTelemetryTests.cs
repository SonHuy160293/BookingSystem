using System.Diagnostics;
using System.Diagnostics.Metrics;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Abstractions.Shared;
using BookingSystem.SharedKernel.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookingSystem.SharedKernel.UnitTests;

public sealed class CqrsTelemetryTests
{
    [Fact]
    public async Task FailedResultRecordsFailureWithoutRequestOrErrorDataAsync()
    {
        var outcomes = new List<string>();
        var durations = new List<double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "BookingSystem.Cqrs")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (tags.ToArray().Any(tag => Equals(tag.Value, nameof(FailedRequest))))
            {
                outcomes.Add(tags.ToArray().Single(tag => tag.Key == "cqrs.outcome").Value!.ToString()!);
                Assert.Equal(2, tags.Length);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (tags.ToArray().Any(tag => Equals(tag.Value, nameof(FailedRequest))))
            {
                durations.Add(measurement);
            }
        });
        listener.Start();
        var behavior = new TracingPipelineBehavior<FailedRequest, Result>(NullLogger<FailedRequest>.Instance);
        var result = await behavior.HandleAsync(
            new FailedRequest("private-password"),
            () => Task.FromResult(Result.Failure(new Error("private-code", "private-error"))),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("failure", Assert.Single(outcomes));
        Assert.True(Assert.Single(durations) >= 0);
    }

    [Fact]
    public async Task ExceptionSetsErrorStatusWithoutExceptionMessageAsync()
    {
        Activity? span = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "BookingSystem.Cqrs",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == nameof(FailedRequest))
                {
                    span = activity;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        var behavior = new TracingPipelineBehavior<FailedRequest, Result>(NullLogger<FailedRequest>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.HandleAsync(
            new FailedRequest("private-password"),
            () => throw new InvalidOperationException("private-error"),
            CancellationToken.None));
        Assert.NotNull(span);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Empty(span.Events);
        Assert.Null(span.StatusDescription);
    }

    private sealed class FailedRequest : IRequest<Result>
    {
        internal FailedRequest(string password)
        {
            Password = password;
        }

        internal string Password { get; }
    }
}
