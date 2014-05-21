// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

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

            scheduleRepository.Stub(x => x.GetPending()).Return(pendingSchedule);

            schedulerService.OnPublishTimerTick(null);

            advancedBus.AssertWasCalled(x => x.Publish(
                Arg<Exchange>.Is.Anything, 
                Arg<string>.Is.Equal("msg1"),
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<MessageProperties>.Is.Anything,
                Arg<byte[]>.Is.Anything));

            advancedBus.AssertWasCalled(x => x.Publish(
                Arg<Exchange>.Is.Anything, 
                Arg<string>.Is.Equal("msg2"),
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<MessageProperties>.Is.Anything,
                Arg<byte[]>.Is.Anything));
        }
    }
}

// ReSharper restore InconsistentNaming