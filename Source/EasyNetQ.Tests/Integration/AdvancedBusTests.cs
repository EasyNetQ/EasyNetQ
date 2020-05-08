using System;
using System.Text;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a RabbitMQ instance on localhost to work")]
    public class AdvancedBusTests : IDisposable
    {
        public void Dispose()
        {
            bus.Dispose();
        }

        private IBus bus;

        [Fact]
        public void Should_be_able_to_declare_exchange_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                var advancedBus = (IAdvancedBus)s;
                advancedBus.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
            });

            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
        }

        [Fact]
        public void Should_be_able_to_declare_queue_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                var advancedBus = (IAdvancedBus)s;
                advancedBus.QueueDeclare("my_test_queue", c => c.AsAutoDelete(true));
            });
            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
        }

        [Fact]
        public void Should_be_able_to_public_message_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                var advancedBus = (IAdvancedBus)s;
                var exchange = advancedBus.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
                advancedBus.Publish(exchange, "key", false, new MessageProperties(), Encoding.UTF8.GetBytes("Hello world"));
            });
            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
        }
    }
}
