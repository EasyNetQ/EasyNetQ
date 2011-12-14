// ReSharper disable InconsistentNaming

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
                new SchedulerServiceConfiguration
                {
                    PublishIntervalSeconds = 1,
                    PurgeIntervalSeconds = 1
                });
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

            scheduleRepository.GetPendingDelegate = () => pendingSchedule;

            rawByteBus.RawPublishDelegate = (typeName, messageBody) =>
                published.Add(typeName);

            schedulerService.OnPublishTimerTick(null);

            published.Count.ShouldEqual(2);
            published[0].ShouldEqual("msg1");
            published[1].ShouldEqual("msg2");
        }
    }
}

// ReSharper restore InconsistentNaming