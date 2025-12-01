using System.Threading;
using EasyNetQ.Logging;

namespace EasyNetQ.Internals;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
public static class Timers
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static IDisposable Start(Func<Task> callback, TimeSpan period, ILogger logger)
    {
        PeriodicTimer timer = new PeriodicTimer(period);
        StartAsync(timer, callback);
        return timer;
    }
    private async static Task StartAsync(PeriodicTimer timer, Func<Task> callback)
    {
        while (await timer.WaitForNextTickAsync())
        {
            await callback();
        }
    }
}
