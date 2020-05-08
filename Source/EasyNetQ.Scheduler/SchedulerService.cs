using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace EasyNetQ.Scheduler
{
    public interface ISchedulerService
    {
        void Start();
        void Stop();
    }

    public class SchedulerService : ISchedulerService
    {
        private readonly ILog log = LogManager.GetLogger(typeof(ScheduleRepository));

        private const string SchedulerSubscriptionId = "schedulerSubscriptionId";

        private readonly IBus bus;
        private readonly IScheduleRepository scheduleRepository;
        private readonly SchedulerServiceConfiguration configuration;

        private System.Threading.Timer publishTimer;
        private System.Threading.Timer purgeTimer;

        public SchedulerService(
            IBus bus,
            IScheduleRepository scheduleRepository,
            SchedulerServiceConfiguration configuration)
        {
            this.bus = bus;
            this.scheduleRepository = scheduleRepository;
            this.configuration = configuration;
        }

        public void Start()
        {
            log.DebugFormat("Starting SchedulerService");

            bus.PubSub.Subscribe<ScheduleMe>(SchedulerSubscriptionId, OnMessage);
            bus.PubSub.Subscribe<UnscheduleMe>(SchedulerSubscriptionId, OnMessage);

            publishTimer = new System.Threading.Timer(OnPublishTimerTick, null, 0, configuration.PublishIntervalSeconds * 1000);
            purgeTimer = new System.Threading.Timer(OnPurgeTimerTick, null, 0, configuration.PurgeIntervalSeconds * 1000);
        }

        public void Stop()
        {
            log.DebugFormat("Stopping SchedulerService");

            publishTimer?.Dispose();
            purgeTimer?.Dispose();
            bus?.Dispose();
        }

        private void OnMessage(ScheduleMe scheduleMe)
        {
            log.DebugFormat("Got Schedule Message");
            scheduleRepository.Store(scheduleMe);
        }

        private void OnMessage(UnscheduleMe unscheduleMe)
        {
            log.DebugFormat("Got Unschedule Message");
            scheduleRepository.Cancel(unscheduleMe);
        }

        public void OnPublishTimerTick(object state)
        {
            try
            {
                if (!bus.Advanced.IsConnected)
                {
                    log.Info("Not connected");
                    return;
                }

                // Keep track of exchanges that have already been declared this tick
                var declaredExchanges = new ConcurrentDictionary<Tuple<string, string>, IExchange>();
                Func<Tuple<string, string>, IExchange> declareExchange = exchangeNameType =>
                {
                    log.DebugFormat("Declaring exchange {0}, {1}", exchangeNameType.Item1, exchangeNameType.Item2);
                    return bus.Advanced.ExchangeDeclare(exchangeNameType.Item1, exchangeNameType.Item2);
                };

                using (var scope = new TransactionScope())
                {
                    var scheduledMessages = scheduleRepository.GetPending();

                    foreach (var scheduledMessage in scheduledMessages)
                    {
                        // Binding key fallback is only provided here for backwards compatibility, will be removed in the future
                        log.DebugFormat("Publishing Scheduled Message with Routing Key: '{0}'", scheduledMessage.BindingKey);

                        var exchangeName = scheduledMessage.Exchange ?? scheduledMessage.BindingKey;
                        var exchangeType = scheduledMessage.ExchangeType ?? ExchangeType.Topic;

                        var exchange = declaredExchanges.GetOrAdd(new Tuple<string, string>(exchangeName, exchangeType), declareExchange);

                        var messageProperties = scheduledMessage.MessageProperties;

                        if (scheduledMessage.MessageProperties == null)
                            messageProperties = new MessageProperties { Type = scheduledMessage.BindingKey };

                        var routingKey = scheduledMessage.RoutingKey ?? scheduledMessage.BindingKey;

                        bus.Advanced.Publish(
                            exchange,
                            routingKey,
                            false,
                            messageProperties,
                            scheduledMessage.InnerMessage
                        );
                    }

                    scope.Complete();
                }
            }
            catch (Exception exception)
            {
                log.Error("Error in schedule poll", exception);
            }
        }

        private void OnPurgeTimerTick(object state)
        {
            try
            {
                scheduleRepository.Purge();
            }
            catch (Exception exception)
            {
                log.Error("Error in schedule purge", exception);
            }
        }
    }
}
