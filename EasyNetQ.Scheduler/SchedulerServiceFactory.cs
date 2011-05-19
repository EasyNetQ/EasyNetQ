using log4net;

namespace EasyNetQ.Scheduler
{
    public static class SchedulerServiceFactory
    {
        public static ISchedulerService CreateScheduler()
        {
            var bus = RabbitHutch.CreateBus();

            var rawByteBus = bus as IRawByteBus;
            if (rawByteBus == null)
            {
                throw new EasyNetQException("Bus does not implement IRawByteBus");
            }

            var log = LogManager.GetLogger("");

            return new SchedulerService(bus, rawByteBus, log, new ScheduleRepository());
        }
    }
}