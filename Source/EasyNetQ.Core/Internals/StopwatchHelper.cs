using System.Diagnostics;

namespace EasyNetQ.Internals;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
internal static class StopwatchHelper
{
#if !NET
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
#endif

    /// <summary>
    ///     Elapsed time since a <see cref="Stopwatch.GetTimestamp" /> value
    /// </summary>
    public static TimeSpan GetElapsedTime(long startingTimestamp)
#if NET
        => Stopwatch.GetElapsedTime(startingTimestamp);
#else
        => GetElapsedTime(startingTimestamp, Stopwatch.GetTimestamp());
#endif

    /// <summary>
    ///     Elapsed time between two <see cref="Stopwatch.GetTimestamp" /> values
    /// </summary>
    public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
#if NET
        => Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);
#else
        => new((long)((endingTimestamp - startingTimestamp) * TimestampToTicks));
#endif
}
