using EasyNetQ.Scheduler.Mongo.Core;
using EasyNetQ.Topology;
using NSubstitute;
using NUnit.Framework;
using System;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    [TestFixture]
    public class SchedulerServiceTests
    {
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
                        HandleTimeoutInterval = TimeSpan.FromSeconds(1),
                        PublishInterval = TimeSpan.FromSeconds(1),
                        SubscriptionId = "Scheduler",
                        PublishMaxSchedules = 2
                    });
        }

        private SchedulerService schedulerService;
        private IBus bus;
        private IAdvancedBus advancedBus;
        private IScheduleRepository scheduleRepository;

        [Test]
        public void Should_get_pending_scheduled_messages_and_update_them()
        {
            var id = Guid.NewGuid();
            scheduleRepository.GetPending().Returns(new Schedule
                {
                    Id = id,
                    BindingKey = "msg1"
                });

            schedulerService.OnPublishTimerTick(null);

            scheduleRepository.Received().MarkAsPublished(id);
            advancedBus.Received().Publish(
                Arg.Any<IExchange>(),
                Arg.Is<string>("msg1"),
                Arg.Any<bool>(),
                Arg.Any<MessageProperties>(),
                Arg.Any<byte[]>());
        }


        [Test]
        public void Should_hadle_publish_timeout_()
        {
            schedulerService.OnHandleTimeoutTimerTick(null);
            scheduleRepository.Received().HandleTimeout();
        }
    }
}

// ReSharper restore InconsistentNaming