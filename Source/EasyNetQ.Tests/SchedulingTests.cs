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
            bus = RabbitHutch.CreateBus("host=localhost");
            while(!bus.IsConnected) Thread.Sleep(10);
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work AND scheduler service running")]
        public void Should_be_able_to_schedule_a_message()
        {
            bus.Subscribe<PartyInvitation>("schedulingTest1", message => 
                Console.WriteLine("Got scheduled message: {0}", message.Text));

            var invitation = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };

            bus.FuturePublish(DateTime.UtcNow.AddSeconds(3), invitation);

            Thread.Sleep(6000);
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