using EasyNetQ.Internals;

namespace EasyNetQ.Tests.Internals;

public class TaskExtensionsTests
{
    [Fact]
    public async Task Should_cancel_when_cancellation_attached()
    {
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.AttachCancellation(cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tcs.Task);
    }
}
