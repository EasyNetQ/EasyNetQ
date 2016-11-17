// ReSharper disable InconsistentNaming

using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Scheduler.Tests
{
    [TestFixture]
    public class SchedulerServiceTests
    {
        private SchedulerService schedulerService;
        private IBus bus;
        private IAdvancedBus advancedBus;
        private IScheduleRepository scheduleRepository;

        [SetUp]
        public void SetUp()
        {
            bus = Substitute.For<IBus>();
            advancedBus = Substitute.For<IAdvancedBus>();

            bus.IsConnected.Returns(true);
            bus.Advanced.Returns(advancedBus);

            scheduleRepository = Substitute.For<IScheduleRepository>();

            schedulerService = new SchedulerService(
                bus, 
                Substitute.For<IEasyNetQLogger>(), 
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
                new ScheduleMe { RoutingKey = "msg1"},
                new ScheduleMe { RoutingKey = "msg2"},
            };

            scheduleRepository.GetPending().Returns(pendingSchedule);

            schedulerService.OnPublishTimerTick(null);

            advancedBus.Received().Publish(
                Arg.Any<IExchange>(),
                Arg.Is<string>("msg1"),
                Arg.Any<bool>(),
                Arg.Any<MessageProperties>(),
                Arg.Any<byte[]>());

            advancedBus.Received().Publish(
                Arg.Any<IExchange>(),
                Arg.Is<string>("msg2"),
                Arg.Any<bool>(),
                Arg.Any<MessageProperties>(),
                Arg.Any<byte[]>());
        }
    }
}

// ReSharper restore InconsistentNaming