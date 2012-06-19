// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PublishSubscribeWithTopicsTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        [TearDown]
        public void TearDown()
        {
            if(bus != null) bus.Dispose();
        }

        private MyMessage CreateMessage()
        {
            return new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) };
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_some_messages_with_topics()
        {
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish("X.A", CreateMessage());
                publishChannel.Publish("X.B", CreateMessage());
                publishChannel.Publish("Y.A", CreateMessage());
            }
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Subscribe_to_messages_with_topics()
        {
            var countdownEvent = new CountdownEvent(7);

            bus.Subscribe<MyMessage>("id1", "X.*", msg =>
            {
                Console.WriteLine("I Get X: {0}", msg.Text);
                countdownEvent.Signal();
            });
            bus.Subscribe<MyMessage>("id2", "*.A", msg =>
            {
                Console.WriteLine("I Get A: {0}", msg.Text);
                countdownEvent.Signal();
            });
            bus.Subscribe<MyMessage>("id3", msg =>
            {
                Console.WriteLine("I Get All: {0}", msg.Text);
                countdownEvent.Signal();
            });

            countdownEvent.Wait(1000);
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_subscribe_to_multiple_topic_strings()
        {
            var countdownEvent = new CountdownEvent(7);
            bus.Subscribe<MyMessage>("id4", new[]{"Y.*", "*.B"}, msg =>
            {
                Console.WriteLine("I Get Y or B: {0}", msg.Text);
                countdownEvent.Signal();
            });

            countdownEvent.Wait(500);
        }
    }
}

// ReSharper restore InconsistentNaming