using System;
using System.Timers;
using System.Transactions;
using EasyNetQ.SystemMessages;
using log4net;

namespace EasyNetQ.Scheduler
{
    public interface ISchedulerService
    {
        void Start();
        void Stop();

    }

    public class SchedulerService : ISchedulerService
    {
        private const string schedulerSubscriptionId = "schedulerSubscriptionId";
        private const int intervalMilliseconds = 2000;

        private readonly IBus bus;
        private readonly IRawByteBus rawByteBus;
        private readonly ILog log;
        private readonly IScheduleRepository scheduleRepository;

        private System.Threading.Timer timer;

        public SchedulerService(IBus bus, IRawByteBus rawByteBus, ILog log, IScheduleRepository scheduleRepository)
        {
            this.bus = bus;
            this.scheduleRepository = scheduleRepository;
            this.rawByteBus = rawByteBus;
            this.log = log;
        }

        public void Start()
        {
            log.Debug("Starting SchedulerService");
            bus.Subscribe<ScheduleMe>(schedulerSubscriptionId, OnMessage);

            timer = new System.Threading.Timer(OnTimerTick, null, 0, intervalMilliseconds);
        }

        public void Stop()
        {
            log.Debug("Stopping SchedulerService");
            if (timer != null)
            {
                timer.Dispose();
            }
            if (bus != null)
            {
                bus.Dispose();
            }
        }

        public void OnMessage(ScheduleMe scheduleMe)
        {
            try
            {
                log.Debug("Got Schedule Message");
                scheduleRepository.Store(scheduleMe);
            }
            catch (Exception exception)
            {
                log.Error("Error receiving message from queue", exception);
            }
        }

        public void OnTimerTick(object state)
        {
            if (!bus.IsConnected) return;
            try
            {
                using(var scope = new TransactionScope())
                {
                    var scheduledMessages = scheduleRepository.GetPending(DateTime.Now);
                    foreach (var scheduledMessage in scheduledMessages)
                    {
                        log.Debug(string.Format(
                            "Publishing Scheduled Message with Routing Key: '{0}'", scheduledMessage.BindingKey));
                        rawByteBus.RawPublish(scheduledMessage.BindingKey, scheduledMessage.InnerMessage);
                    }

                    scope.Complete();
                }
            }
            catch (Exception exception)
            {
                log.Error("Error in schedule pol", exception);
            }
        }
    }
}