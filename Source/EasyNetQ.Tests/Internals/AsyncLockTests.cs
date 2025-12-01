using EasyNetQ.Internals;

namespace EasyNetQ.Tests.Internals;

public class AsyncLockTests
{
    [Fact]
    public async Task Should_throw_oce_if_already_cancelled()
    {
        using var mutex = new AsyncLock();
        using var cts = new CancellationTokenSource();

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mutex.AcquireAsync(cts.Token));
    }

    [Fact]
    public async Task Should_throw_oce_if_cancelled()
    {
        using var mutex = new AsyncLock();
        using var cts = new CancellationTokenSource();

        using var releaser = await mutex.AcquireAsync(CancellationToken.None);

        var acquireAsync = mutex.AcquireAsync(cts.Token);
        acquireAsync.IsCompleted.Should().BeFalse();

        cts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquireAsync);
    }
}
