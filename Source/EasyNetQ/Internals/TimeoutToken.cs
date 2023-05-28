using System.Diagnostics;

namespace EasyNetQ.Internals;

public readonly struct TimeoutToken
{
    private readonly long startedAt;
    private readonly TimeSpan timeout;

    private TimeoutToken(long startedAt, TimeSpan timeout)
    {
        this.startedAt = startedAt;
        this.timeout = timeout;
    }

    public TimeSpan Remaining
    {
        get
        {
            if (startedAt == 0) return Timeout.InfiniteTimeSpan;

            var remaining = timeout - GetElapsedTime(startedAt);
            return remaining >= TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public bool Expired
    {
        get
        {
            if (startedAt == 0) return false;

            return timeout - GetElapsedTime(startedAt) <= TimeSpan.Zero;
        }
    }

    public TimeSpan TryAcquire(TimeSpan needed)
    {
        if (needed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), $"Negative needed value: '{needed}'");

        if (startedAt == 0) return needed;

        var remaining = timeout - GetElapsedTime(startedAt);
        if (remaining <= TimeSpan.Zero) return TimeSpan.Zero;

        return needed <= remaining ? needed : remaining;
    }

    public static TimeoutToken StartNew(TimeSpan timeout)
    {
        if (timeout == Timeout.InfiniteTimeSpan) return default;

        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), $"Negative time budget value: '{timeout}'");

        return new TimeoutToken(Stopwatch.GetTimestamp(), timeout);
    }

    public static implicit operator TimeoutToken(TimeSpan timeout) => StartNew(timeout);

    public static implicit operator TimeSpan(TimeoutToken timeoutToken) => timeoutToken.Remaining;

    public static TimeoutToken None => default;

    private static TimeSpan GetElapsedTime(long startedAt)
    {
        var elapsed = Stopwatch.GetTimestamp() - startedAt;
        return TimeSpan.FromTicks(elapsed * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
    }
}
