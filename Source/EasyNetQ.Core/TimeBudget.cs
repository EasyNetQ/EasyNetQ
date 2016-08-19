using System;
using System.Diagnostics;

namespace EasyNetQ
{
    public sealed class TimeBudget
    {
        public TimeBudget(TimeSpan budget, TimeSpan precision)
        {
            this.budget = budget;
            this.precision = precision;
            watch = new Stopwatch();
        }

        public TimeBudget(TimeSpan budget)
            : this(budget, TimeSpan.FromMilliseconds(5)) { }

        public TimeBudget Start()
        {
            watch.Start();
            return this;
        }

        public TimeSpan GetRemainingTime()
        {
            var remaining = budget - watch.Elapsed;
            return remaining < precision
                ? TimeSpan.Zero
                : remaining;
        }

        public bool IsExpired()
        {
            var remaining = budget - watch.Elapsed;
            return remaining < precision;
        }

        private readonly TimeSpan budget;
        private readonly TimeSpan precision;
        private readonly Stopwatch watch;
    }
}