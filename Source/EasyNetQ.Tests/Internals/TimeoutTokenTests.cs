using EasyNetQ.Internals;

namespace EasyNetQ.Tests.Internals;

public class TimeoutTokenTests
{
    [Fact]
    public void Should_be_infinite_by_default()
    {
        var timeoutToken = TimeoutToken.None;
        timeoutToken.Expired.Should().BeFalse();
        timeoutToken.Remaining.Should().Be(Timeout.InfiniteTimeSpan);
        timeoutToken.Should().Be(default(TimeoutToken));
    }

    [Fact]
    public async Task Should_work_with_positive_timeout()
    {
        var timeout = TimeSpan.FromSeconds(0.1);

        var timeoutToken = TimeoutToken.StartNew(timeout);
        timeoutToken.Expired.Should().BeFalse();
        timeoutToken.Remaining.Should().BeCloseTo(timeout, timeout / 2);

        await Task.Delay(timeout);

        timeoutToken.Expired.Should().BeTrue();
        timeoutToken.Remaining.Should().Be(TimeSpan.Zero);

        await Task.Delay(timeout);

        timeoutToken.Expired.Should().BeTrue();
        timeoutToken.Remaining.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Should_be_expired_if_timeout_zero()
    {
        var timeoutToken = TimeoutToken.StartNew(TimeSpan.Zero);
        timeoutToken.Expired.Should().BeTrue();
        timeoutToken.Remaining.Should().Be(TimeSpan.Zero);
    }
}
