using EasyNetQ.Internals;

namespace EasyNetQ.Tests.Internals;

public class TaskExtensionsTests
{
    [Fact]
    public async Task Should_cancelled_when_cancellation_requested_later()
    {
        using var cts = new CancellationTokenSource(50);
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.AttachTimeoutAndCancellation(Timeout.InfiniteTimeSpan, cts.Token);

        tcs.Task.IsCanceled.Should().BeFalse();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tcs.Task);
    }

    [Fact]
    public async Task Should_cancelled_instantly_if_cancellation_requested()
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.AttachTimeoutAndCancellation(Timeout.InfiniteTimeSpan, new CancellationToken(true));

        tcs.Task.IsCanceled.Should().BeTrue();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tcs.Task);
    }

    [Fact]
    public async Task Should_timed_out_instantly_if_timeout_zero()
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.AttachTimeoutAndCancellation(TimeSpan.Zero, CancellationToken.None);

        tcs.Task.IsFaulted.Should().BeTrue();
        await Assert.ThrowsAnyAsync<TimeoutException>(() => tcs.Task);
    }

    [Fact]
    public async Task Should_timed_out_if_timeout_gte_zero()
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.AttachTimeoutAndCancellation(TimeSpan.FromMilliseconds(50), CancellationToken.None);

        tcs.Task.IsFaulted.Should().BeFalse();
        await Assert.ThrowsAnyAsync<TimeoutException>(() => tcs.Task);
    }
}
