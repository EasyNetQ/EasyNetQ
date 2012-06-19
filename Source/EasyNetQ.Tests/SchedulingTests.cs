// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using EasyNetQ.Loggers;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class SchedulingTests
    {
        private IBus bus;
        private ConsoleLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = new ConsoleLogger();
            bus = RabbitHutch.CreateBus("host=localhost", logger);
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        /// <summary>
        /// Tests the EasyNetQ.Scheduler.
        /// 1. First run EasyNetQ.Scheduler.exe
        /// 2. Run this test. You should see the test reporting that it's published a ScheduleMe message
        /// and then the Scheduler reporting 'Got Schedule Message'. 3 seconds later you should see the 
        /// scheduler publish a PartyInvitation message and the test should report 
        /// 'Got scheduled message: Please come to my party'
        /// </summary>
        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work AND scheduler service running")]
        public void Should_be_able_to_schedule_a_message()
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
            
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.FuturePublish(DateTime.UtcNow.AddSeconds(3), invitation);
            }

            autoResetEvent.WaitOne(10000);
        }

        /// <summary>
        /// Schedule thousands of messages
        /// 1. First run EasyNetQ.Scheduler.exe
        /// 2. Run this test.
        /// </summary>
        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work AND scheduler service running")]
        public void High_volume_scheduling_test()
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

                using (var publishChannel = bus.OpenPublishChannel())
                {
                    publishChannel.FuturePublish(DateTime.UtcNow.AddSeconds(3), invitation);
                }
                Thread.Sleep(1);
            }
        }
    }

    [Serializable]
    public class PartyInvitation
    {
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
}

// ReSharper restore InconsistentNaming