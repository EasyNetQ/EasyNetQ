using System;
using System.Threading;
using EasyNetQ.Scheduler.Mongo.Core.Logging;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduler.Mongo.Core
{
    public class SchedulerService : ISchedulerService
    {
        private readonly ILog logger = LogProvider.For<SchedulerService>();
        
        private readonly IBus bus;
        private readonly ISchedulerServiceConfiguration configuration;
        private readonly IScheduleRepository scheduleRepository;
        private Timer handleTimeoutTimer;
        private Timer publishTimer;

        public SchedulerService(IBus bus, IScheduleRepository scheduleRepository, ISchedulerServiceConfiguration configuration)
        {
            this.bus = bus;
            this.scheduleRepository = scheduleRepository;
            this.configuration = configuration;
        }

        public void Start()
        {
            logger.Debug("Starting SchedulerService");
            bus.Subscribe<ScheduleMe>(configuration.SubscriptionId, OnMessage);
            bus.Subscribe<UnscheduleMe>(configuration.SubscriptionId, OnMessage);
            publishTimer = new Timer(OnPublishTimerTick, null, TimeSpan.Zero, configuration.PublishInterval);
            handleTimeoutTimer = new Timer(OnHandleTimeoutTimerTick, null, TimeSpan.Zero, configuration.HandleTimeoutInterval);
        }

        public void Stop()
        {
            logger.Debug("Stopping SchedulerService");
            if (publishTimer != null)
            {
                publishTimer.Dispose();
            }
            if (handleTimeoutTimer != null)
            {
                handleTimeoutTimer.Dispose();
            }
            if (bus != null)
            {
                bus.Dispose();
            }
        }

        public void OnHandleTimeoutTimerTick(object state)
        {
            logger.Debug("Handling failed messages");
            scheduleRepository.HandleTimeout();
        }

        public void OnPublishTimerTick(object state)
        {
            if (!bus.IsConnected) return;
            try
            {
                var published = 0;
                while (published < configuration.PublishMaxSchedules)
                {
                    var schedule = scheduleRepository.GetPending();
                    if (schedule == null)
                        return;
                    var exchangeName = schedule.Exchange ?? schedule.BindingKey;
                    var routingKey = schedule.RoutingKey ?? schedule.BindingKey;
                    var properties = schedule.BasicProperties ?? new MessageProperties {Type = schedule.BindingKey};
                    logger.DebugFormat("Publishing Scheduled Message with to exchange '{0}'", exchangeName);
                    var exchange = bus.Advanced.ExchangeDeclare(exchangeName, schedule.ExchangeType ?? ExchangeType.Topic);
                    bus.Advanced.Publish(
                        exchange,
                        routingKey,
                        false,
                        properties,
                        schedule.InnerMessage);
                    scheduleRepository.MarkAsPublished(schedule.Id);
                    ++published;
                }
            }
            catch (Exception exception)
            {
                logger.ErrorFormat("Error in schedule pol\r\n{0}", exception);
            }
        }

        private void OnMessage(UnscheduleMe message)
        {
            logger.Debug("Got Unschedule Message");
            scheduleRepository.Cancel(message.CancellationKey);
        }

        private void OnMessage(ScheduleMe message)
        {
            logger.Debug("Got Schedule Message");
            scheduleRepository.Store(new Schedule
            {
                Id = Guid.NewGuid(),
                CancellationKey = message.CancellationKey,
                BindingKey = message.BindingKey,
                InnerMessage = message.InnerMessage,
                State = ScheduleState.Pending,
                WakeTime = message.WakeTime,
                Exchange = message.Exchange,
                ExchangeType = message.ExchangeType,
                RoutingKey = message.RoutingKey,
                BasicProperties = message.MessageProperties
            });
        }
    }
}