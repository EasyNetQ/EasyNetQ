using System;
using EasyNetQ.Scheduler.Mongo.Core;
using log4net;

namespace EasyNetQ.Scheduler.Mongo
{
    public static class SchedulerServiceFactory
    {
        public static ISchedulerService CreateScheduler()
        {
            var bus = RabbitHutch.CreateBus();
            var logger = new Logger(LogManager.GetLogger("EasyNetQ.Scheduler"));

            return new SchedulerService(
                bus,
                logger,
                new ScheduleRepository(ScheduleRepositoryConfiguration.FromConfigFile(), () => DateTime.UtcNow),
                SchedulerServiceConfiguration.FromConfigFile());
        }
    }
}