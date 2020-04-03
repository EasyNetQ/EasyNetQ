using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using Xunit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NSubstitute;

namespace EasyNetQ.Tests
{
    public class AdvancedBusEventHandlersTests
    {
        private AdvancedBusEventHandlers advancedBusEventHandlers;
        private IEventBus eventBus;
        private bool connectedCalled = false;
        private bool disconnectedCalled = false;
        private bool blockedCalled = false;
        private ConnectionBlockedEventArgs connectionBlockedEventArgs;
        private bool unBlockedCalled = false;
        private bool messageReturnedCalled = false;
        private MessageReturnedEventArgs messageReturnedEventArgs;
        private ConnectedEventArgs connectedEventArgs;
        private DisconnectedEventArgs disconnectedEventArgs;

        public AdvancedBusEventHandlersTests()
        {
            advancedBusEventHandlers = new AdvancedBusEventHandlers(
                connected: (s, e) =>
                {
                    connectedCalled = true;
                    connectedEventArgs = e;
                },
                disconnected: (s, e) =>
                {
                    disconnectedCalled = true;
                    disconnectedEventArgs = e;
                },
                blocked: (s, e) =>
                {
                    blockedCalled = true;
                    connectionBlockedEventArgs = e;
                },
                unblocked: (s, e) => unBlockedCalled = true,
                messageReturned: (s, e) =>
                {
                    messageReturnedCalled = true;
                    messageReturnedEventArgs = e;
                });

            var connectionFactory = Substitute.For<IConnectionFactory>();
            connectionFactory.Succeeded.Returns(true);
            var connection = Substitute.For<IConnection>();
            connection.Endpoint.Returns(new AmqpTcpEndpoint());
            connectionFactory.CreateConnection().Returns(connection);
            connectionFactory.CurrentHost.Returns(new HostConfiguration());
            connectionFactory.Configuration.Returns(new ConnectionConfiguration());
            

            eventBus = new EventBus();

            var persistentConnectionFactory = new PersistentConnectionFactory(connectionFactory, eventBus);            

            var advancedBus = new RabbitAdvancedBus(
                connectionFactory,
                Substitute.For<IConsumerFactory>(),
                Substitute.For<IClientCommandDispatcherFactory>(),
                Substitute.For<IPublishConfirmationListener>(),
                eventBus,
                Substitute.For<IHandlerCollectionFactory>(),
                Substitute.For<IServiceResolver>(),
                Substitute.For<ConnectionConfiguration>(),
                Substitute.For<IProduceConsumeInterceptor>(),
                Substitute.For<IMessageSerializationStrategy>(),
                Substitute.For<IConventions>(),
                advancedBusEventHandlers,
                persistentConnectionFactory);
        }

        [Fact]
        public void AdvancedBusEventHandlers_Connected_handler_is_called_when_advancedbus_connects_for_the_first_time()
        {
            Assert.True(connectedCalled, "The AdvancedBusEventHandlers Connected event handler wasn't called during RabbitAdvancedBus instantiation.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_Connected_handler_is_called()
        {
            eventBus.Publish(new ConnectionCreatedEvent("localhost", 5672));
            Assert.True(connectedCalled, "The AdvancedBusEventHandlers Connected event handler wasn't called after a ConnectionCreatedEvent publish.");
            Assert.Equal("localhost", connectedEventArgs.Hostname);
            Assert.Equal(5672, connectedEventArgs.Port);
        }

        [Fact]
        public void AdvancedBusEventHandlers_Disconnected_handler_is_called()
        {
            eventBus.Publish(new ConnectionDisconnectedEvent("localhost", 5672, "reason for disconnect"));
            Assert.True(disconnectedCalled, "The AdvancedBusEventHandlers Disconnected event handler wasn't called after a ConnectionDisconnectedEvent publish.");
            Assert.Equal("localhost", disconnectedEventArgs.Hostname);
            Assert.Equal(5672, disconnectedEventArgs.Port);
            Assert.Equal("reason for disconnect", disconnectedEventArgs.Reason);
        }

        [Fact]
        public void AdvancedBusEventHandlers_Blocked_handler_is_called()
        {
            var connectionBlockedEvent = new ConnectionBlockedEvent("a random reason");

            eventBus.Publish(connectionBlockedEvent);
            Assert.True(blockedCalled, "The AdvancedBusEventHandlers Blocked event handler wasn't called after a ConnectionBlockedEvent publish.");
            Assert.NotNull(connectionBlockedEventArgs); //, "The AdvancedBusEventHandlers Blocked event handler received a null ConnectionBlockedEventArgs");
            Assert.True(connectionBlockedEvent.Reason == connectionBlockedEventArgs.Reason, "The published ConnectionBlockedEvent Reason isn't the same object than the one received in AdvancedBusEventHandlers Blocked ConnectionBlockedEventArgs.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_Unblocked_handler_is_called()
        {
            eventBus.Publish(new ConnectionUnblockedEvent());
            Assert.True(unBlockedCalled, "The AdvancedBusEventHandlers Unblocked event handler wasn't called after a ConnectionUnblockedEvent publish.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_MessageReturned_handler_is_called()
        {
            var returnedMessageEvent = new ReturnedMessageEvent(new byte[0], new MessageProperties(), new MessageReturnedInfo("my.exchange", "routing.key", "reason"));

            eventBus.Publish(returnedMessageEvent);
            Assert.True(messageReturnedCalled, "The AdvancedBusEventHandlers MessageReturned event handler wasn't called after a ReturnedMessageEvent publish.");
            Assert.NotNull(messageReturnedEventArgs); //, "The AdvancedBusEventHandlers MessageReturned event handler received a null MessageReturnedEventArgs.");
            Assert.True(returnedMessageEvent.Body == messageReturnedEventArgs.MessageBody, "The published ReturnedMessageEvent Body isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
            Assert.True(returnedMessageEvent.Properties == messageReturnedEventArgs.MessageProperties, "The published ReturnedMessageEvent Properties isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
            Assert.True(returnedMessageEvent.Info == messageReturnedEventArgs.MessageReturnedInfo, "The published ReturnedMessageEvent Info isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
        }
    }
}
