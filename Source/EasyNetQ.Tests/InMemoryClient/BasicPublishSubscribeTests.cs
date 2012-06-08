// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.InMemoryClient;
using NUnit.Framework;

namespace EasyNetQ.Tests.InMemoryClient
{
    [TestFixture]
    public class BasicPublishSubscribeTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = InMemoryRabbitHutch.CreateBus();
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_be_able_to_publish()
        {
            var publishedMessage = new MyMessage {Text = "Hello There Fido!"};
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(publishedMessage);
            }
        }

        [Test]
        public void Should_be_able_to_subscribe()
        {
            bus.Subscribe<MyMessage>("subscriberId", message => Console.WriteLine("Got message: {0}", message.Text));
        }

        [Test]
        public void Should_be_able_to_publish_and_subscribe()
        {
            MyMessage receivedMessage = null;
            bus.Subscribe<MyMessage>("subscriberId", message =>
            {
                Console.WriteLine("Got message {0}", message.Text);
                receivedMessage = message;
            });

            var publishedMessage = new MyMessage { Text = "Hello There Fido!" };
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(publishedMessage);
            }

            // give the task background thread time to process the message.
            Thread.Sleep(100);

            receivedMessage.Text.ShouldEqual(publishedMessage.Text);
        }
    }
}

// ReSharper restore InconsistentNaming