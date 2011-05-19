// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class SchedulingTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("localhost");
        }

        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work AND scheduler service running")]
        public void Should_be_able_to_schedule_a_message()
        {
            bus.Subscribe<MessageToBeScheduled>("schedulingTest1", message => 
                Console.WriteLine("Got scheduled message: {0}", message.Text));

            bus.Schedule(DateTime.Now.AddSeconds(3), new MessageToBeScheduled { Text = "Hi!"});

            Thread.Sleep(6000);
        }
    }

    [Serializable]
    public class MessageToBeScheduled
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming