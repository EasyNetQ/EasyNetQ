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
                Console.WriteLine("Got message '{0}'", message.Text);
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

        [Test]
        public void Should_load_share_between_multiple_consumers()
        {
            MyMessage receivedMessage1 = null;
            bus.Subscribe<MyMessage>("subscriberId", message =>
            {
                Console.WriteLine("Handler A got '{0}'", message.Text);
                receivedMessage1 = message;
            });

            MyMessage receivedMessage2 = null;
            bus.Subscribe<MyMessage>("subscriberId", message =>
            {
                Console.WriteLine("Handler B got '{0}'", message.Text);
                receivedMessage2 = message;
            });

            var publishedMessage1 = new MyMessage { Text = "Hello There From The First!" };
            var publishedMessage2 = new MyMessage { Text = "Hello There From The Second!" };
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(publishedMessage1);
                channel.Publish(publishedMessage2);
            }

            // give the task background thread time to process the message.
            Thread.Sleep(100);

            receivedMessage1.Text.ShouldEqual(publishedMessage1.Text);
            receivedMessage2.Text.ShouldEqual(publishedMessage2.Text);
        }
    }
}

// ReSharper restore InconsistentNaming