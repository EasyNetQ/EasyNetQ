using System;
using EasyNetQ.Scheduler.Mongo.Core;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    [TestFixture]
    public class SchedulerServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            bus = MockRepository.GenerateStub<IBus>();
            advancedBus = MockRepository.GenerateStub<IAdvancedBus>();

            bus.Stub(x => x.IsConnected).Return(true);
            bus.Stub(x => x.Advanced).Return(advancedBus);

            scheduleRepository = MockRepository.GenerateStub<IScheduleRepository>();

            schedulerService = new SchedulerService(
                bus,
                MockRepository.GenerateStub<IEasyNetQLogger>(),
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
            scheduleRepository.Stub(x => x.GetPending()).Return(new Schedule
                {
                    Id = id,
                    BindingKey = "msg1"
                });

            schedulerService.OnPublishTimerTick(null);

            scheduleRepository.AssertWasCalled(x => x.MarkAsPublished(id));
            advancedBus.AssertWasCalled(x => x.Publish(
                Arg<Exchange>.Is.Anything,
                Arg<string>.Is.Equal("msg1"),
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<MessageProperties>.Is.Anything,
                Arg<byte[]>.Is.Anything));
        }


        [Test]
        public void Should_hadle_publish_timeout_()
        {
            schedulerService.OnHandleTimeoutTimerTick(null);
            scheduleRepository.AssertWasCalled(x => x.HandleTimeout());
        }
    }
}

// ReSharper restore InconsistentNaming