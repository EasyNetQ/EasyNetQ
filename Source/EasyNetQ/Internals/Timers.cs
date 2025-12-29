using Microsoft.Extensions.Logging;

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
#if NET8_0_OR_GREATER
        PeriodicTimer timer = new PeriodicTimer(period);
#pragma warning disable CS4014 // Don't dispose injected
        StartAsync(timer, callback, logger);
#pragma warning restore CS4014 // Don't dispose injected
        return timer;
#else
        var callbackLock = new object();
        var timer = new Timer(_ =>
        {
            if (!Monitor.TryEnter(callbackLock)) return;
            try
            {
                callback.Invoke();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error from timer callback");
            }
            finally
            {
                Monitor.Exit(callbackLock);
            }
        });
        timer.Change(period, period);
        return timer;
#endif
    }
#if NET8_0_OR_GREATER
    private async static Task StartAsync(PeriodicTimer timer, Func<Task> callback, ILogger logger)
    {
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
            await callback();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error from timer callback");
            }
        }
    }
#endif
}
