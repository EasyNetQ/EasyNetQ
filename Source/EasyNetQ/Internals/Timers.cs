using System;
using System.Threading;

namespace EasyNetQ.Internals
{
    //https://stackoverflow.com/questions/4962172/why-does-a-system-timers-timer-survive-gc-but-not-system-threading-timer
    public static class Timers
    {
        public static IDisposable Start(TimerCallback callback, TimeSpan dueTime, TimeSpan period)
        {
            var callbackLock = new object();
            var timer = new Timer(state =>
            {
                if (!Monitor.TryEnter(callbackLock)) return;
                try
                {
                    callback.Invoke(state);
                }
                finally
                {
                    Monitor.Exit(callbackLock);
                }
            });
            timer.Change(dueTime, period);
            return timer;
        }

        public static void RunOnce(TimerCallback callback, TimeSpan dueTime)
        {
            var timer = new Timer(state =>
            {
                ((Timer) state).Dispose();
                callback(state);
            });
            timer.Change(dueTime, Timeout.InfiniteTimeSpan);
        }
    }
}