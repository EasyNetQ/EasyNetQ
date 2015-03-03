using System.Text;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ instance on localhost to work")]
    public class AdvancedBusTests
    {
        private IBus bus;

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_be_able_to_declare_exchange_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers(connected: (s, e) =>
                {
                    var advancedBus = ((IAdvancedBus)s);
                    advancedBus.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
                });
            bus = RabbitHutch.CreateBus("host=localhost", advancedBusEventHandlers);
        }

        [Test]
        public void Should_be_able_to_declare_queue_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers(connected: (s, e) =>
            {
                var advancedBus = ((IAdvancedBus)s);
                advancedBus.QueueDeclare("my_test_queue", autoDelete: true);
            });
            bus = RabbitHutch.CreateBus("host=localhost", advancedBusEventHandlers);
        }

        [Test]
        public void Should_be_able_to_public_message_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers(connected: (s, e) =>
            {
                var advancedBus = ((IAdvancedBus)s);
                var exchange = advancedBus.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
                advancedBus.Publish(exchange, "key", false, false, new MessageProperties(), Encoding.UTF8.GetBytes("Hello world"));
            });
            bus = RabbitHutch.CreateBus("host=localhost", advancedBusEventHandlers);
        }
    }
}