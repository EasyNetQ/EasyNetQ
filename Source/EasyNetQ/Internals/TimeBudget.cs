using System.Diagnostics;

namespace EasyNetQ.Internals;

public readonly struct TimeBudget
{
    public static TimeBudget Expired => default;

    private static readonly TimeSpan DefaultPrecision = TimeSpan.FromMilliseconds(1);
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long startedAt;
    private readonly TimeSpan total;
    private readonly TimeSpan precision;

    private TimeBudget(long startedAt, TimeSpan total, TimeSpan precision)
    {
        if (total < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(total), $"Negative time budget value: '{total}'");

        if (precision < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(precision), $"Negative precision value: '{precision}'");

        this.startedAt = startedAt;
        this.total = total;
        this.precision = precision;
    }

    public TimeSpan Total => total;

    public TimeSpan Remaining
    {
        get
        {
            var remaining = total - GetElapsedTime(startedAt);
            return remaining >= TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public bool IsExpired => total - GetElapsedTime(startedAt) - precision <= TimeSpan.Zero;

    public static TimeBudget Start(TimeSpan budget) => new(Stopwatch.GetTimestamp(), budget, DefaultPrecision);
    private static TimeSpan GetElapsedTime(long start)
    {
        var timestampDelta = Stopwatch.GetTimestamp() - start;
        var ticks = (long)(TimestampToTicks * timestampDelta);
        return new TimeSpan(ticks);
    }
}
