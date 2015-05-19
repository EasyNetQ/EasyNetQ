using System;
using System.Threading;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduler.Mongo.Core
{
    public class SchedulerService : ISchedulerService
    {
        private readonly IBus bus;
        private readonly ISchedulerServiceConfiguration configuration;
        private readonly IEasyNetQLogger log;
        private readonly IScheduleRepository scheduleRepository;
        private Timer handleTimeoutTimer;
        private Timer publishTimer;

        public SchedulerService(IBus bus, IEasyNetQLogger log, IScheduleRepository scheduleRepository, ISchedulerServiceConfiguration configuration)
        {
            this.bus = bus;
            this.log = log;
            this.scheduleRepository = scheduleRepository;
            this.configuration = configuration;
        }

        public void Start()
        {
            log.DebugWrite("Starting SchedulerService");
            bus.Subscribe<ScheduleMe>(configuration.SubscriptionId, OnMessage);
            bus.Subscribe<UnscheduleMe>(configuration.SubscriptionId, OnMessage);
            publishTimer = new Timer(OnPublishTimerTick, null, TimeSpan.Zero, configuration.PublishInterval);
            handleTimeoutTimer = new Timer(OnHandleTimeoutTimerTick, null, TimeSpan.Zero, configuration.HandleTimeoutInterval);
        }

        public void Stop()
        {
            log.DebugWrite("Stopping SchedulerService");
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
            log.DebugWrite("Handling failed messages");
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
                    log.DebugWrite(string.Format("Publishing Scheduled Message with to exchange '{0}'", exchangeName));
                    var exchange = bus.Advanced.ExchangeDeclare(exchangeName, schedule.ExchangeType ?? ExchangeType.Topic);
                    bus.Advanced.Publish(
                        exchange,
                        routingKey,
                        false,
                        false,
                        properties,
                        schedule.InnerMessage);
                    scheduleRepository.MarkAsPublished(schedule.Id);
                    ++published;
                }
            }
            catch (Exception exception)
            {
                log.ErrorWrite("Error in schedule pol\r\n{0}", exception);
            }
        }

        private void OnMessage(UnscheduleMe message)
        {
            log.DebugWrite("Got Unschedule Message");
            scheduleRepository.Cancel(message.CancellationKey);
        }

        private void OnMessage(ScheduleMe message)
        {
            log.DebugWrite("Got Schedule Message");
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