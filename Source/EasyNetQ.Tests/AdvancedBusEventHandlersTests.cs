using System;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests
{
    public class AdvancedBusEventHandlersTests : IDisposable
    {
        public AdvancedBusEventHandlersTests()
        {
            var advancedBusEventHandlers = new AdvancedBusEventHandlers(
                (s, e) =>
                {
                    connectedCalled = true;
                    connectedEventArgs = e;
                },
                (s, e) =>
                {
                    disconnectedCalled = true;
                    disconnectedEventArgs = e;
                },
                (s, e) =>
                {
                    blockedCalled = true;
                    blockedEventArgs = e;
                },
                (s, e) => unBlockedCalled = true,
                (s, e) =>
                {
                    messageReturnedCalled = true;
                    messageReturnedEventArgs = e;
                }
            );

            eventBus = new EventBus();

            advancedBus = new RabbitAdvancedBus(
                Substitute.For<IPersistentConnection>(),
                Substitute.For<IConsumerFactory>(),
                Substitute.For<IClientCommandDispatcher>(),
                Substitute.For<IPublishConfirmationListener>(),
                eventBus,
                Substitute.For<IHandlerCollectionFactory>(),
                Substitute.For<IServiceResolver>(),
                Substitute.For<ConnectionConfiguration>(),
                Substitute.For<IProduceConsumeInterceptor>(),
                Substitute.For<IMessageSerializationStrategy>(),
                Substitute.For<IConventions>(),
                Substitute.For<IPullingConsumerFactory>(),
                advancedBusEventHandlers
            );
        }

        public void Dispose()
        {
            advancedBus.Dispose();
        }

        private readonly IEventBus eventBus;
        private bool connectedCalled;
        private bool disconnectedCalled;
        private bool blockedCalled;
        private BlockedEventArgs blockedEventArgs;
        private bool unBlockedCalled;
        private bool messageReturnedCalled;
        private MessageReturnedEventArgs messageReturnedEventArgs;
        private readonly IAdvancedBus advancedBus;
        private ConnectedEventArgs connectedEventArgs;
        private DisconnectedEventArgs disconnectedEventArgs;

        [Fact]
        public void AdvancedBusEventHandlers_Blocked_handler_is_called()
        {
            var @event = new ConnectionBlockedEvent("a random reason");

            eventBus.Publish(@event);
            blockedCalled.Should().BeTrue();
            blockedEventArgs.Should().NotBeNull();
            blockedEventArgs.Reason.Should().Be(@event.Reason);
        }

        [Fact]
        public void AdvancedBusEventHandlers_Connected_handler_is_called_when_connection_recovered()
        {
            eventBus.Publish(new ConnectionRecoveredEvent(new AmqpTcpEndpoint()));
            connectedCalled.Should().BeTrue();
            connectedEventArgs.Hostname.Should().Be("localhost");
            connectedEventArgs.Port.Should().Be(5672);
        }

        [Fact]
        public void AdvancedBusEventHandlers_Connected_handler_is_called_when_connection_created()
        {
            eventBus.Publish(new ConnectionCreatedEvent(new AmqpTcpEndpoint()));
            connectedCalled.Should().BeTrue();
            connectedEventArgs.Hostname.Should().Be("localhost");
            connectedEventArgs.Port.Should().Be(5672);
        }

        [Fact]
        public void AdvancedBusEventHandlers_Disconnected_handler_is_called()
        {
            var @event = new ConnectionDisconnectedEvent(new AmqpTcpEndpoint(), "a random reason");
            eventBus.Publish(@event);
            disconnectedCalled.Should().BeTrue();
            disconnectedEventArgs.Should().NotBeNull();
            disconnectedEventArgs.Hostname.Should().Be("localhost");
            disconnectedEventArgs.Port.Should().Be(5672);
            disconnectedEventArgs.Reason.Should().Be("a random reason");
        }

        [Fact]
        public void AdvancedBusEventHandlers_MessageReturned_handler_is_called()
        {
            var @event = new ReturnedMessageEvent(
                null, new byte[0], new MessageProperties(), new MessageReturnedInfo("my.exchange", "routing.key", "reason")
            );

            eventBus.Publish(@event);
            messageReturnedCalled.Should().BeTrue();
            messageReturnedEventArgs.Should().NotBeNull();
            messageReturnedEventArgs.MessageBody.Should().Equal(@event.Body);
            messageReturnedEventArgs.MessageProperties.Should().Be(@event.Properties);
            messageReturnedEventArgs.MessageReturnedInfo.Should().Be(@event.Info);
         }

        [Fact]
        public void AdvancedBusEventHandlers_Unblocked_handler_is_called()
        {
            eventBus.Publish(new ConnectionUnblockedEvent());
            unBlockedCalled.Should().BeTrue();
        }
    }
}
