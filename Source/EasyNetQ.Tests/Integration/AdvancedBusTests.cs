using System.Text;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ instance on localhost to work")]
    public class AdvancedBusTests
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
            bus.Dispose();
        }

        [Test]
        public void Should_be_able_to_declare_exchange_during_first_on_connected_event()
        {
            bus.Advanced.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
        }

        [Test]
        public void Should_be_able_to_declare_queue_during_first_on_connected_event()
        {
            bus.Advanced.QueueDeclare("my_test_queue", autoDelete: true);
        }

        [Test]
        public void Should_be_able_to_public_message_during_first_on_connected_event()
        {
            var exchange = bus.Advanced.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
            bus.Advanced.Publish(exchange, "key", false, false, new MessageProperties(), Encoding.UTF8.GetBytes("Hello world"));
        }
    }
}