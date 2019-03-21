using System;

namespace EasyNetQ.Scheduler
{
    public static class SchedulerServiceFactory
    {
        public static ISchedulerService CreateScheduler()
        {
            var serviceConfig = SchedulerServiceConfiguration.FromConfigFile();
            var bus = RabbitHutch.CreateBus(sr =>
            {
                if (serviceConfig.EnableLegacyConventions)
                {
                    sr.EnableLegacyConventions();
                }
            });
            return new SchedulerService(
                bus,
                new ScheduleRepository(ScheduleRepositoryConfiguration.FromConfigFile(), () => DateTime.UtcNow),
                SchedulerServiceConfiguration.FromConfigFile());
        }
    }
}