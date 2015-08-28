using System;
using System.Diagnostics;

namespace EasyNetQ
{
    public class TimeBudget
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

        public TimeBudget Stop()
        {
            watch.Stop();
            return this;
        }

        public TimeSpan Remaining()
        {
            var remaining = budget - watch.Elapsed;
            return remaining < precision
                ? TimeSpan.Zero
                : remaining;
        }

        public TimeSpan Elapsed()
        {
            return watch.Elapsed;
        }

        public bool HasExpired()
        {
            var remaining = budget - watch.Elapsed;
            return remaining < precision;
        }

        public TimeSpan Budget
        {
            get { return budget; }
        }

        public TimeSpan Precision
        {
            get { return precision; }
        }

        private readonly TimeSpan budget;
        private readonly TimeSpan precision;
        private readonly Stopwatch watch;
    }
}