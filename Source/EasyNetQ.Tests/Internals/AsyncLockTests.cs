using EasyNetQ.Internals;

namespace EasyNetQ.Tests.Internals;

public class AsyncLockTests
{
    [Fact]
    public async Task Should_throw_timeout_if_cannot_acquire_in_time()
    {
        using var mutex = new AsyncLock();
        using var releaser = await mutex.AcquireAsync(CancellationToken.None);

        mutex.Acquired.Should().BeTrue();
        await Assert.ThrowsAnyAsync<TimeoutException>(() => mutex.AcquireAsync(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Should_acquire_if_timeout_provided()
    {
        using var mutex = new AsyncLock();
        using var releaser = await mutex.AcquireAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        mutex.Acquired.Should().BeTrue();
    }


    [Fact]
    public async Task Should_throw_oce_if_already_cancelled()
    {
        using var mutex = new AsyncLock();

        mutex.Acquired.Should().BeFalse();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mutex.AcquireAsync(new CancellationToken(true)));
    }

    [Fact]
    public async Task Should_throw_oce_if_cancelled()
    {
        using var mutex = new AsyncLock();
        using var cts = new CancellationTokenSource();

        using var releaser = await mutex.AcquireAsync(CancellationToken.None);

        var acquireAsync = mutex.AcquireAsync(cts.Token);

        mutex.Acquired.Should().BeTrue();
        acquireAsync.IsCompleted.Should().BeFalse();

        cts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquireAsync);
    }
}
