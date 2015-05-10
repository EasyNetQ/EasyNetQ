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
            var serviceConfiguration = SchedulerServiceConfiguration.FromConfigFile();
            var repositoryConfiguration = ScheduleRepositoryConfiguration.FromConfigFile();
            Func<DateTime> now = () => DateTime.UtcNow;

            var schedulerService = new SchedulerService(
                bus,
                logger,
                new ScheduleRepository(repositoryConfiguration, now),
                serviceConfiguration);
            var schedulerV2Service = new ScheduleV2Service(
                bus,
                logger,
                new ScheduleV2Repository(repositoryConfiguration, now),
                serviceConfiguration);

            return new CompositeSchedulerService(schedulerService, schedulerV2Service);
        }

        private class CompositeSchedulerService : ISchedulerService
        {
            private readonly ISchedulerService[] services;

            public CompositeSchedulerService(params ISchedulerService[] services)
            {
                this.services = services;
            }

            public void Start()
            {
                foreach (var service in services)
                    service.Start();
            }

            public void Stop()
            {
                foreach (var service in services)
                    service.Stop();
            }
        }
    }
}