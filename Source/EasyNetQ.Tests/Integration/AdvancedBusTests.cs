using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
                var advancedBus = (IAdvancedBus) s;
                advancedBus.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
            });

            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
        }

        [Fact]
        public void Should_be_able_to_declare_queue_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                var advancedBus = (IAdvancedBus) s;
                advancedBus.QueueDeclare("my_test_queue", c => c.AsAutoDelete(true));
            });
            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
        }

        [Fact]
        public void Should_be_able_to_public_message_during_first_on_connected_event()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                var advancedBus = (IAdvancedBus) s;
                var exchange = advancedBus.ExchangeDeclare("my_test_exchange", "topic", autoDelete: true);
                advancedBus.Publish(exchange, "key", false, new MessageProperties(), Encoding.UTF8.GetBytes("Hello world"));
            });
            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
        }
        [Fact]
        public void Should_get_connected_eventArgs()
        {
            var autoResetEvent = new AutoResetEvent(false);
            ConnectedEventArgs connectedEventArgs = null;
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                connectedEventArgs = e;
                autoResetEvent.Set();
            });
            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));

            var done = autoResetEvent.WaitOne(TimeSpan.FromSeconds(1));

            done.Should().BeTrue("AutoResetEvent should not not have timed out.");
            connectedEventArgs.Hostname.Should().Be("localhost");
        }

        [Fact]
        public void Should_get_disconnected_event_after_disconnection_on_dispose_in_connected_event()
        {
            var connectedResetEvent = new AutoResetEvent(false);
            var disconnectedResetEvent = new AutoResetEvent(false);
            DisconnectedEventArgs disconnectedEventArgs = null;
            var advancedBusEventHandlers = new AdvancedBusEventHandlers((s, e) =>
            {
                connectedResetEvent.Set();
            }, (s, e) =>
            {
                disconnectedEventArgs = e;
                disconnectedResetEvent.Set();
            });
            bus = RabbitHutch.CreateBus("host=localhost", c => c.Register(advancedBusEventHandlers));
            bool doneConnecting = connectedResetEvent.WaitOne(TimeSpan.FromSeconds(2));
            doneConnecting.Should().BeTrue("Should have received connected event");
            
            bool doneDisconnecting = disconnectedResetEvent.WaitOne(TimeSpan.FromSeconds(2));
            doneDisconnecting.Should().BeTrue("Should have disposed connection from connected event");
            disconnectedEventArgs.Hostname.Should().Be("localhost");
            disconnectedEventArgs.Reason.Should().NotBeNull();
        }
    }
}
