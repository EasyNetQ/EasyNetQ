// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using EasyNetQ.Loggers;
using EasyNetQ.SystemMessages;
using NUnit.Framework;

namespace EasyNetQ.Scheduler.Tests
{
    [TestFixture]
    public class SchedulerServiceTests
    {
        private SchedulerService schedulerService;
        private MockBus bus;
        private MockRawByteBus rawByteBus;
        private MockScheduleRepository scheduleRepository;
        private readonly DateTime now = new DateTime(2010, 8, 18);

        [SetUp]
        public void SetUp()
        {
            bus = new MockBus();
            rawByteBus = new MockRawByteBus();
            scheduleRepository = new MockScheduleRepository();

            schedulerService = new SchedulerService(
                bus, 
                rawByteBus, 
                new ConsoleLogger(), 
                scheduleRepository, 
                () => now);
        }

        [Test]
        public void Should_get_pending_scheduled_messages_and_update_them()
        {
            var pendingSchedule = new List<ScheduleMe>
            {
                new ScheduleMe { BindingKey = "msg1"},
                new ScheduleMe { BindingKey = "msg2"},
            };

            var published = new List<string>();

            scheduleRepository.GetPendingDelegate = timeNow =>
            {
                timeNow.ShouldEqual(now);
                return pendingSchedule;
            };

            rawByteBus.RawPublishDelegate = (typeName, messageBody) =>
                published.Add(typeName);

            schedulerService.OnTimerTick(null);

            published.Count.ShouldEqual(2);
            published[0].ShouldEqual("msg1");
            published[1].ShouldEqual("msg2");
        }
    }
}

// ReSharper restore InconsistentNaming