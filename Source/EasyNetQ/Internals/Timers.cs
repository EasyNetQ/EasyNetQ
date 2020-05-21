using System;
using System.Threading;
using EasyNetQ.Logging;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static class Timers
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(Timers));

        /// <summary>
        /// Runs a callback in timer with a lock between invocations of a callback
        /// </summary>
        /// <param name="callback">The callback to run</param>
        /// <param name="dueTime">A <see cref="T:System.TimeSpan"></see> representing the amount of time to delay before invoking the callback. Specify negative one (-1) milliseconds to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback. Specify negative one (-1) milliseconds to disable periodic signaling.</param>
        /// <returns></returns>
        public static IDisposable Start(Action callback, TimeSpan dueTime, TimeSpan period)
        {
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
                    logger.Error(exception, string.Empty);
                }
                finally
                {
                    Monitor.Exit(callbackLock);
                }
            });
            timer.Change(dueTime, period);
            return timer;
        }
    }
}
