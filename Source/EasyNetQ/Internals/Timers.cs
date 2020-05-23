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
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
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
