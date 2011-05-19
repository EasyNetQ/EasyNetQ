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

        private System.Timers.Timer timer;

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

            timer = new System.Timers.Timer(intervalMilliseconds);
            timer.Elapsed += OnTimerTick;

            timer.Enabled = true;
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Enabled = false;
            }
        }

        public void OnMessage(ScheduleMe scheduleMe)
        {
            Console.WriteLine("Got Schedule Message");
            scheduleRepository.Store(scheduleMe);
        }

        public void OnTimerTick(object source, ElapsedEventArgs args)
        {
            using(var scope = new TransactionScope())
            {
                var scheduledMessages = scheduleRepository.GetPending(DateTime.Now);
                foreach (var scheduledMessage in scheduledMessages)
                {
                    Console.WriteLine("Publishing Scheduled Message with Routing Key: '{0}'", scheduledMessage.BindingKey);
                    rawByteBus.RawPublish(scheduledMessage.BindingKey, scheduledMessage.InnerMessage);
                }

                scope.Complete();
            }
        }
    }
}