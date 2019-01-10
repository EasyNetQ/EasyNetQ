// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Producer;
using EasyNetQ.Scheduling;
using Xunit;

namespace EasyNetQ.Tests.Integration.Scheduling
{
    [Explicit("Requires RabbitMQ instance to be running on localhost AND rabbitmq_delayed_message_exchange plugin to be installed.")]
    public class DelayedExchangeSchedulerTests : IDisposable
    {
        private IBus bus;

        public DelayedExchangeSchedulerTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost", x => x.Register<IScheduler, DelayedExchangeScheduler>());
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_be_able_to_schedule_a_message_with_delay()
        {
            var autoResetEvent = new AutoResetEvent(false);

            bus.PubSub.Subscribe<PartyInvitation>("schedulingTest1", message =>
            {
                Console.WriteLine("Got scheduled message: {0}", message.Text);
                autoResetEvent.Set();
            });
            var invitation = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };

            bus.Scheduler.FuturePublish(invitation, TimeSpan.FromSeconds(3));

            if (!autoResetEvent.WaitOne(100000))
                Assert.True(false);
        }

        [Fact]
        public void High_volume_scheduling_test_with_delay()
        {
            bus.PubSub.Subscribe<PartyInvitation>("schedulingTest1", message =>
                Console.WriteLine("Got scheduled message: {0}", message.Text));

            var count = 0;
            while (true)
            {
                var invitation = new PartyInvitation
                {
                    Text = string.Format("Invitation {0}", count++),
                    Date = new DateTime(2011, 5, 24)
                };

                bus.Scheduler.FuturePublish(invitation, TimeSpan.FromSeconds(3));
                Thread.Sleep(1);
            }
        }
    }
}

// ReSharper restore InconsistentNaming