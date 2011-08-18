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
        private const int intervalMilliseconds = 2000;

        private readonly IBus bus;
        private readonly IRawByteBus rawByteBus;
        private readonly IEasyNetQLogger log;
        private readonly IScheduleRepository scheduleRepository;
        private readonly Func<DateTime> now; 

        private System.Threading.Timer timer;

        public SchedulerService(
            IBus bus, 
            IRawByteBus rawByteBus,
            IEasyNetQLogger log, 
            IScheduleRepository scheduleRepository, 
            Func<DateTime> now)
        {
            this.bus = bus;
            this.scheduleRepository = scheduleRepository;
            this.now = now;
            this.rawByteBus = rawByteBus;
            this.log = log;
        }

        public void Start()
        {
            log.DebugWrite("Starting SchedulerService");
            bus.Subscribe<ScheduleMe>(schedulerSubscriptionId, OnMessage);

            timer = new System.Threading.Timer(OnTimerTick, null, 0, intervalMilliseconds);
        }

        public void Stop()
        {
            log.DebugWrite("Stopping SchedulerService");
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
                log.DebugWrite("Got Schedule Message");
                scheduleRepository.Store(scheduleMe);
            }
            catch (Exception exception)
            {
                log.ErrorWrite("Error receiving message from queue", exception);
            }
        }

        public void OnTimerTick(object state)
        {
            if (!bus.IsConnected) return;
            try
            {
                using(var scope = new TransactionScope())
                {
                    var scheduledMessages = scheduleRepository.GetPending(now());
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
    }
}