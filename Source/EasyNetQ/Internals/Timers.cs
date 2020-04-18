using System;
using System.Threading;
using EasyNetQ.Logging;

namespace EasyNetQ.Internals
{
    //https://stackoverflow.com/questions/4962172/why-does-a-system-timers-timer-survive-gc-but-not-system-threading-timer
    public static class Timers
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(Timers));

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
