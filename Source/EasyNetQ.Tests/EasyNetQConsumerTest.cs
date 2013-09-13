using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class EasyNetQConsumerTest
    {
        private IModel channelMock;

        [SetUp]
        public void SetUp()
        {
            // Mocks
            channelMock = MockRepository.GenerateMock<IModel>();
        }

        [Test]
        public void Consumer_should_fire_event_basicCancel_when_consumer_cancel_notification_is_received()
        {
            // SetUp
            const string consumerTag = "consumerTag";
            var consumer = new EasyNetQConsumer(channelMock, args => {});

            var receivedConsumerTag = string.Empty;

            consumer.BasicCancel += (sender, args) => { receivedConsumerTag = args.ConsumerTag; };

            // Act
            consumer.HandleBasicCancel(consumerTag); // Imitating Consumer Cancelation Notification

            // Validate
            Assert.AreEqual(consumerTag, receivedConsumerTag);
        }
    }
}