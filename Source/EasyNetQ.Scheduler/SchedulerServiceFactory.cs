using System;
using log4net;

namespace EasyNetQ.Scheduler
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

    public class Logger : IEasyNetQLogger
    {
        private readonly ILog log;
        public Logger(ILog log)
        {
            this.log = log;
        }

        public void DebugWrite(string format, params object[] args)
        {
            log.DebugFormat(format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            log.InfoFormat(format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            log.ErrorFormat(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            log.ErrorFormat(exception.ToString());
        }
    }
}