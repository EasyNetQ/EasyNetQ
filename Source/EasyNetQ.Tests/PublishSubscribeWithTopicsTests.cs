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
            while (!bus.IsConnected) Thread.Sleep(10);
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

        [Test]
        public void Publish_some_messages_with_topics()
        {
            bus.Publish("X.A", CreateMessage());
            bus.Publish("X.B", CreateMessage());
            bus.Publish("Y.A", CreateMessage());
        }

        [Test]
        public void Subscribe_to_messages_with_topics()
        {
            bus.Subscribe<MyMessage>("id1", "X.*", msg => Console.WriteLine("I Get X: {0}", msg.Text));
            bus.Subscribe<MyMessage>("id2", "*.A", msg => Console.WriteLine("I Get A: {0}", msg.Text));
            bus.Subscribe<MyMessage>("id3", msg => Console.WriteLine("I Get All: {0}", msg.Text));

            Thread.Sleep(500);
        }

        [Test]
        public void Should_subscribe_to_multiple_topic_strings()
        {
            bus.Subscribe<MyMessage>("id4", new[]{"Y.*", "*.B"}, msg => Console.WriteLine("I Get Y or B: {0}", msg.Text));

            Thread.Sleep(500);
        }
    }
}

// ReSharper restore InconsistentNaming