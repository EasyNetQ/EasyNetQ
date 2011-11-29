using System.Diagnostics;

namespace Mike.AmqpSpike
{
    public class PerformanceCounterSpike
    {
        public void CreatePerformanceCategory()
        {
            const string category = "MikePerfSpike";

            if (!PerformanceCounterCategory.Exists(category))
            {
                var counters = new CounterCreationDataCollection();

                // 1. counter for counting values
                var totalOps = new CounterCreationData
                {
                    CounterName = "# of operations executed",
                    CounterHelp = "Total number of operations that have been executed",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalOps);

                // 2. counter for counting operations per second
                var opsPerSecond = new CounterCreationData
                {
                    CounterName = "# of operations/second",
                    CounterHelp = "Number of operations per second",
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                };
                counters.Add(opsPerSecond);

                PerformanceCounterCategory.Create(
                    category,
                    "An experiment",
                    PerformanceCounterCategoryType.MultiInstance,
                    counters);
            }
        } 
    }
}