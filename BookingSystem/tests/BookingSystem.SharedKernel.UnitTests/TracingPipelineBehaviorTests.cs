using System.Diagnostics;
using BookingSystem.SharedKernel.Abstractions.Message;
using BookingSystem.SharedKernel.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookingSystem.SharedKernel.UnitTests;

public sealed class TracingPipelineBehaviorTests
{
    [Fact]
    public async Task HandleAsyncCreatesCqrsSpanWithoutRequestDataAsync()
    {
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TracingPipelineBehavior<SecretRequest, string>.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == nameof(SecretRequest))
                {
                    stoppedActivity = activity;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new TracingPipelineBehavior<SecretRequest, string>(NullLogger<SecretRequest>.Instance);
        var result = await behavior.HandleAsync(new SecretRequest("private-value"), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.NotNull(stoppedActivity);
        Assert.Equal(nameof(SecretRequest), stoppedActivity.OperationName);
        Assert.Equal("success", stoppedActivity.GetTagItem("cqrs.outcome"));
        Assert.DoesNotContain(stoppedActivity.TagObjects, tag => tag.Value?.ToString() == "private-value");
    }

    private sealed class SecretRequest : IRequest<string>
    {
        public SecretRequest(string password)
        {
            Password = password;
        }

        public string Password { get; }
    }
}
