using System;
using System.Transactions;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

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

        private readonly IBus bus;
        private readonly IEasyNetQLogger log;
        private readonly IScheduleRepository scheduleRepository;
        private readonly SchedulerServiceConfiguration configuration;

        private System.Threading.Timer publishTimer;
        private System.Threading.Timer purgeTimer;

        public SchedulerService(
            IBus bus, 
            IEasyNetQLogger log, 
            IScheduleRepository scheduleRepository, 
            SchedulerServiceConfiguration configuration)
        {
            this.bus = bus;
            this.scheduleRepository = scheduleRepository;
            this.configuration = configuration;
            this.log = log;
        }

        public void Start()
        {
            log.DebugWrite("Starting SchedulerService");
            bus.Subscribe<ScheduleMe>(schedulerSubscriptionId, OnMessage);

            publishTimer = new System.Threading.Timer(OnPublishTimerTick, null, 0, configuration.PublishIntervalSeconds * 1000);
            purgeTimer = new System.Threading.Timer(OnPurgeTimerTick, null, 0, configuration.PurgeIntervalSeconds * 1000);
        }

        public void Stop()
        {
            log.DebugWrite("Stopping SchedulerService");
            if (publishTimer != null)
            {
                publishTimer.Dispose();
            }
            if (purgeTimer != null)
            {
                purgeTimer.Dispose();
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
                using(var channel = bus.Advanced.OpenPublishChannel())
                {
                    var scheduledMessages = scheduleRepository.GetPending();
                    foreach (var scheduledMessage in scheduledMessages)
                    {
                        log.DebugWrite(string.Format(
                            "Publishing Scheduled Message with Routing Key: '{0}'", scheduledMessage.BindingKey));

                        var exchange = Exchange.DeclareTopic(scheduledMessage.BindingKey);
                        channel.Publish(
                            exchange, 
                            scheduledMessage.BindingKey, 
                            new MessageProperties{ Type = scheduledMessage.BindingKey }, 
                            scheduledMessage.InnerMessage);
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