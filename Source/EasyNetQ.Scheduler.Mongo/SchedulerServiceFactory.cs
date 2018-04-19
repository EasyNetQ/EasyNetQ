using System;
using EasyNetQ.Scheduler.Mongo.Core;
using log4net;

namespace EasyNetQ.Scheduler.Mongo
{
    public static class SchedulerServiceFactory
    {
        public static ISchedulerService CreateScheduler()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            return new SchedulerService(
                bus,
                new ScheduleRepository(ScheduleRepositoryConfiguration.FromConfigFile(), () => DateTime.UtcNow),
                SchedulerServiceConfiguration.FromConfigFile());
        }
    }
}