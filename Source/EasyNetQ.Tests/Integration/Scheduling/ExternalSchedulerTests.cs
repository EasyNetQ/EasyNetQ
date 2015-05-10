// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.Scheduling;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration.Scheduling
{
    [TestFixture, Explicit("Needs an instance of RabbitMQ on localhost to work AND scheduler service running")]
    public class ExternalSchedulerTests
    {
        private IBus bus;
        private ConsoleLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = new ConsoleLogger();
            bus = RabbitHutch.CreateBus("host=localhost", x =>
            {
                x.Register<IEasyNetQLogger>(_ => logger);
                x.Register<IScheduler, ExternalScheduler>();
            });
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_be_able_to_schedule_a_message_with_delay()
        {
            var autoResetEvent = new AutoResetEvent(false);

            bus.Subscribe<PartyInvitation>("schedulingTest1", message =>
            {
                Console.WriteLine("Got scheduled message: {0}", message.Text);
                autoResetEvent.Set();
            });
            var invitation = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };

            bus.FuturePublish(TimeSpan.FromSeconds(3), invitation);

            if(! autoResetEvent.WaitOne(100000))
                Assert.Fail();
        }

        [Test]
        public void High_volume_scheduling_test_with_delay()
        {
            logger.Debug = false;

            bus.Subscribe<PartyInvitation>("schedulingTest1", message =>
                Console.WriteLine("Got scheduled message: {0}", message.Text));

            var count = 0;
            while (true)
            {
                var invitation = new PartyInvitation
                {
                    Text = string.Format("Invitation {0}", count++),
                    Date = new DateTime(2011, 5, 24)
                };

                bus.FuturePublish(TimeSpan.FromSeconds(3), invitation);
                Thread.Sleep(1);
            }
        }


        [Test]
        public void Should_be_able_to_schedule_a_message_with_future_date()
        {
            var autoResetEvent = new AutoResetEvent(false);

            bus.Subscribe<PartyInvitation>("schedulingTest1", message =>
            {
                Console.WriteLine("Got scheduled message: {0}", message.Text);
                autoResetEvent.Set();
            });

            var invitation = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };

            bus.FuturePublish(DateTime.UtcNow.AddSeconds(3), invitation);

            autoResetEvent.WaitOne(10000);
        }

        [Test]
        public void High_volume_scheduling_test_with_future_date()
        {
            logger.Debug = false;

            bus.Subscribe<PartyInvitation>("schedulingTest1", message => 
                Console.WriteLine("Got scheduled message: {0}", message.Text));

            var count = 0;
            while (true)
            {
                var invitation = new PartyInvitation
                {
                    Text = string.Format("Invitation {0}", count++),
                    Date = new DateTime(2011, 5, 24)
                };

                bus.FuturePublish(DateTime.UtcNow.AddSeconds(3), invitation);
                Thread.Sleep(1);
            }
        }


        [Test]
        public void Should_be_able_to_cancel_a_message()
        {
            var messagesReceived = 0;

            bus.Subscribe<PartyInvitation>("schedulingTest1", message =>
            {
                Console.WriteLine("Got scheduled message: {0}", message.Text);
                messagesReceived++;
            });

            var invitation = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };

            bus.FuturePublish(DateTime.UtcNow.AddSeconds(3), "my_cancellation_key", invitation);
            bus.FuturePublish(DateTime.UtcNow.AddSeconds(3), invitation);
            bus.FuturePublish(DateTime.UtcNow.AddSeconds(3), "my_cancellation_key", invitation);
            bus.CancelFuturePublish("my_cancellation_key");

            Thread.Sleep(10000);
            Assert.AreEqual(1, messagesReceived);
        }
    }
}

// ReSharper restore InconsistentNaming