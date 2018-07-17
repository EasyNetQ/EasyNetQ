using System;
using System.Diagnostics;
using System.Threading;

namespace EasyNetQ
{
    public sealed class TimeBudget
    {   
        private static readonly TimeSpan Precision = TimeSpan.FromMilliseconds(1);
        
        private readonly TimeSpan budget;
        private readonly Stopwatch watch;

        private TimeBudget(Stopwatch watch, TimeSpan budget)
        {
            this.watch = watch;
            this.budget = budget;
        }

        public static TimeBudget Infinite()
        {
            return new TimeBudget(Stopwatch.StartNew(), Timeout.InfiniteTimeSpan);
        }

        public static TimeBudget Start(TimeSpan budget)
        {
            return new TimeBudget(Stopwatch.StartNew(), budget);
        }

        public bool IsExpired()
        {
            if (budget == Timeout.InfiniteTimeSpan)
            {
                return false;
            }

            var remaining = budget - watch.Elapsed;
            return remaining < Precision;
        }

        public static implicit operator TimeSpan(TimeBudget source)
        {
            if (source.budget == Timeout.InfiniteTimeSpan)
            {
                return Timeout.InfiniteTimeSpan;
            }

            var remaining = source.budget - source.watch.Elapsed;
            return remaining < Precision ? TimeSpan.Zero : remaining;
        }
    }
}