using System.Collections.Concurrent;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class EasyNetQConsumerTest
    {
        private IModel channelMock;
        private BlockingCollection<BasicDeliverEventArgs> queue;

        [SetUp]
        public void SetUp()
        {
            // Objects
            queue = new BlockingCollection<BasicDeliverEventArgs>();

            // Mocks
            channelMock = MockRepository.GenerateMock<IModel>();
        }

        [Test]
        public void Consumer_should_fire_event_basicCancel_when_consumer_cancel_notification_is_received()
        {
            // SetUp
            const string consumerTag = "consumerTag";
            var consumer = new EasyNetQConsumer(channelMock, queue);

            var receivedConsumerTag = string.Empty;

            consumer.BasicCancel += (sender, args) => { receivedConsumerTag = args.ConsumerTag; };

            // Act
            consumer.HandleBasicCancel(consumerTag); // Imitating Consumer Cancelation Notification

            // Validate
            Assert.AreEqual(consumerTag, receivedConsumerTag);
        }
    }
}