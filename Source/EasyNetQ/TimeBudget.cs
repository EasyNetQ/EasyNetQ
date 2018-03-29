using System;
using System.Diagnostics;

namespace EasyNetQ
{
    public sealed class TimeBudget
    {
        private readonly TimeSpan budget;
        private readonly TimeSpan precision;
        private readonly Stopwatch watch;

        private TimeBudget(Stopwatch watch, TimeSpan budget, TimeSpan precision)
        {
            this.watch = watch;
            this.budget = budget;
            this.precision = precision;
        }

        public static TimeBudget Start(TimeSpan budget)
        {
            return new TimeBudget(Stopwatch.StartNew(), budget, TimeSpan.FromMilliseconds(1));
        }

        public bool IsExpired()
        {
            var remaining = budget - watch.Elapsed;
            return remaining < precision;
        }

        public static implicit operator TimeSpan(TimeBudget source)
        {
            var remaining = source.budget - source.watch.Elapsed;
            return remaining < source.precision ? TimeSpan.Zero : remaining;
        }
    }
}