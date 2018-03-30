using System;

namespace EasyNetQ.Scheduler
{
    public static class SchedulerServiceFactory
    {
        public static ISchedulerService CreateScheduler()
        {
            var bus = RabbitHutch.CreateBus();
            return new SchedulerService(
                bus, 
                new ScheduleRepository(ScheduleRepositoryConfiguration.FromConfigFile(), () => DateTime.UtcNow),
                SchedulerServiceConfiguration.FromConfigFile());
        }
    }
}