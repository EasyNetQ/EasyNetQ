﻿// ReSharper disable InconsistentNaming

using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using NSubstitute;
using Xunit;
using System.Collections.Generic;

namespace EasyNetQ.Scheduler.Tests
{
    public class SchedulerServiceTests
    {
        private SchedulerService schedulerService;
        private IBus bus;
        private IAdvancedBus advancedBus;
        private IScheduleRepository scheduleRepository;

        public SchedulerServiceTests()
        {
            bus = Substitute.For<IBus>();
            advancedBus = Substitute.For<IAdvancedBus>();

            advancedBus.IsConnected.Returns(true);
            bus.Advanced.Returns(advancedBus);

            scheduleRepository = Substitute.For<IScheduleRepository>();

            schedulerService = new SchedulerService(
                bus, 
                scheduleRepository,
                new SchedulerServiceConfiguration
                {
                    PublishIntervalSeconds = 1,
                    PurgeIntervalSeconds = 1
                }
            );
        }

        [Fact]
        public void Should_get_pending_scheduled_messages_and_update_them()
        {
            var pendingSchedule = new List<ScheduleMe>
            {
                new ScheduleMe { RoutingKey = "msg1"},
                new ScheduleMe { RoutingKey = "msg2"},
            };

            scheduleRepository.GetPending().Returns(pendingSchedule);

            schedulerService.OnPublishTimerTick(null);

            advancedBus.Received().PublishAsync(
                Arg.Any<IExchange>(),
                Arg.Is("msg1"),
                Arg.Any<bool>(),
                Arg.Any<MessageProperties>(),
                Arg.Any<byte[]>()
            );

            advancedBus.Received().PublishAsync(
                Arg.Any<IExchange>(),
                Arg.Is("msg2"),
                Arg.Any<bool>(),
                Arg.Any<MessageProperties>(),
                Arg.Any<byte[]>()
            );
        }
    }
}

// ReSharper restore InconsistentNaming