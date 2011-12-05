using System;
using System.Transactions;
using EasyNetQ.SystemMessages;

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
        private const int publishInterval = 2000;
        private const int purgeInterval = 2000;

        private readonly IBus bus;
        private readonly IRawByteBus rawByteBus;
        private readonly IEasyNetQLogger log;
        private readonly IScheduleRepository scheduleRepository;

        private System.Threading.Timer publishTimer;
        private System.Threading.Timer purgeTimer;

        public SchedulerService(
            IBus bus, 
            IRawByteBus rawByteBus,
            IEasyNetQLogger log, 
            IScheduleRepository scheduleRepository)
        {
            this.bus = bus;
            this.scheduleRepository = scheduleRepository;
            this.rawByteBus = rawByteBus;
            this.log = log;
        }

        public void Start()
        {
            log.DebugWrite("Starting SchedulerService");
            bus.Subscribe<ScheduleMe>(schedulerSubscriptionId, OnMessage);

            publishTimer = new System.Threading.Timer(OnPublishTimerTick, null, 0, publishInterval);
            purgeTimer = new System.Threading.Timer(OnPurgeTimerTick, null, 0, purgeInterval);
        }

        public void Stop()
        {
            log.DebugWrite("Stopping SchedulerService");
            if (publishTimer != null)
            {
                publishTimer.Dispose();
            }
            if (bus != null)
            {
                bus.Dispose();
            }
        }

        public void OnMessage(ScheduleMe scheduleMe)
        {
            log.DebugWrite("Got Schedule Message");
            scheduleRepository.Store(scheduleMe);
        }

        public void OnPublishTimerTick(object state)
        {
            if (!bus.IsConnected) return;
            try
            {
                using(var scope = new TransactionScope())
                {
                    var scheduledMessages = scheduleRepository.GetPending();
                    foreach (var scheduledMessage in scheduledMessages)
                    {
                        log.DebugWrite(string.Format(
                            "Publishing Scheduled Message with Routing Key: '{0}'", scheduledMessage.BindingKey));
                        rawByteBus.RawPublish(scheduledMessage.BindingKey, scheduledMessage.InnerMessage);
                    }

                    scope.Complete();
                }
            }
            catch (Exception exception)
            {
                log.ErrorWrite("Error in schedule pol\r\n{0}", exception);
            }
        }

        private void OnPurgeTimerTick(object state)
        {
            scheduleRepository.Purge();
        }
    }
}