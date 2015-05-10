using System;
using System.Threading;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduler.Mongo.Core
{
    public class ScheduleV2Service : ISchedulerService
    {
        private readonly IBus bus;
        private readonly ISchedulerServiceConfiguration configuration;
        private readonly IEasyNetQLogger log;
        private readonly IScheduleV2Repository scheduleRepository;
        private Timer handleTimeoutTimer;
        private Timer publishTimer;

        public ScheduleV2Service(IBus bus, IEasyNetQLogger log, IScheduleV2Repository scheduleRepository, ISchedulerServiceConfiguration configuration)
        {
            this.bus = bus;
            this.log = log;
            this.scheduleRepository = scheduleRepository;
            this.configuration = configuration;
        }

        public void Start()
        {
            log.DebugWrite("Starting SchedulerService");
            bus.Subscribe<ScheduleMeV2>(configuration.SubscriptionId, OnMessage);
            bus.Subscribe<UnscheduleMeV2>(configuration.SubscriptionId, OnMessage);
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
                    var exchange = bus.Advanced.ExchangeDeclare(schedule.Exchange, schedule.ExchangeType);
                    bus.Advanced.Publish(
                        exchange,
                        schedule.RoutingKey,
                        false,
                        false,
                        schedule.BasicProperties,
                        schedule.Message);
                    scheduleRepository.MarkAsPublished(schedule.Id);
                    ++published;
                }
            }
            catch (Exception exception)
            {
                log.ErrorWrite("Error in schedule pol\r\n{0}", exception);
            }
        }

        private void OnMessage(UnscheduleMeV2 message)
        {
            log.DebugWrite("Got Unschedule Message");
            scheduleRepository.Cancel(message.CancellationKey);
        }

        private void OnMessage(ScheduleMeV2 message)
        {
            log.DebugWrite("Got Schedule Message");
            scheduleRepository.Store(new ScheduleV2
            {
                Id = Guid.NewGuid(),
                CancellationKey = message.CancellationKey,
                WakeTime = message.WakeTime,
                State = ScheduleState.Pending,
                Exchange = message.Exchange,
                ExchangeType = message.ExchangeType,
                RoutingKey = message.RoutingKey,
                BasicProperties = message.MessageProperties,
                Message = message.Message
            });
        }
    }
}