using System;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
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
                (s, e) => connectedCalled = true,
                (s, e) => disconnectedCalled = true,
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

        [Fact]
        public void AdvancedBusEventHandlers_Blocked_handler_is_called()
        {
            var connectionBlockedEvent = new ConnectionBlockedEvent("a random reason");

            eventBus.Publish(connectionBlockedEvent);
            Assert.True(blockedCalled,
                "The AdvancedBusEventHandlers Blocked event handler wasn't called after a ConnectionBlockedEvent publish.");
            Assert.NotNull(blockedEventArgs); //, "The AdvancedBusEventHandlers Blocked event handler received a null ConnectionBlockedEventArgs");
            Assert.True(connectionBlockedEvent.Reason == blockedEventArgs.Reason,
                "The published ConnectionBlockedEvent Reason isn't the same object than the one received in AdvancedBusEventHandlers Blocked ConnectionBlockedEventArgs.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_Connected_handler_is_called()
        {
            eventBus.Publish(new ConnectionConnectedEvent(new AmqpTcpEndpoint()));
            Assert.True(connectedCalled,
                "The AdvancedBusEventHandlers Connected event handler wasn't called after a ConnectionCreatedEvent publish.");
        }

        [Fact(Skip =
            "Due to creation of connection outside of RabbitAdvancedBus, RabbitAdvancedBus and AdvancedBusHandlers wouldn't receive ConnectionCreatedEvent, because both of them subscribed to the event after creation of connection.")]
        public void AdvancedBusEventHandlers_Connected_handler_is_called_when_advancedbus_connects_for_the_first_time()
        {
            Assert.True(connectedCalled,
                "The AdvancedBusEventHandlers Connected event handler wasn't called during RabbitAdvancedBus instantiation.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_Disconnected_handler_is_called()
        {
            eventBus.Publish(new ConnectionDisconnectedEvent(new AmqpTcpEndpoint(), "Reason"));
            Assert.True(disconnectedCalled,
                "The AdvancedBusEventHandlers Disconnected event handler wasn't called after a ConnectionDisconnectedEvent publish.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_MessageReturned_handler_is_called()
        {
            var returnedMessageEvent = new ReturnedMessageEvent(
                new byte[0], new MessageProperties(), new MessageReturnedInfo("my.exchange", "routing.key", "reason")
            );

            eventBus.Publish(returnedMessageEvent);
            Assert.True(messageReturnedCalled,
                "The AdvancedBusEventHandlers MessageReturned event handler wasn't called after a ReturnedMessageEvent publish.");
            Assert.NotNull(
                messageReturnedEventArgs); //, "The AdvancedBusEventHandlers MessageReturned event handler received a null MessageReturnedEventArgs.");
            Assert.True(returnedMessageEvent.Body == messageReturnedEventArgs.MessageBody,
                "The published ReturnedMessageEvent Body isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
            Assert.True(returnedMessageEvent.Properties == messageReturnedEventArgs.MessageProperties,
                "The published ReturnedMessageEvent Properties isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
            Assert.True(returnedMessageEvent.Info == messageReturnedEventArgs.MessageReturnedInfo,
                "The published ReturnedMessageEvent Info isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
        }

        [Fact]
        public void AdvancedBusEventHandlers_Unblocked_handler_is_called()
        {
            eventBus.Publish(new ConnectionUnblockedEvent());
            Assert.True(unBlockedCalled,
                "The AdvancedBusEventHandlers Unblocked event handler wasn't called after a ConnectionUnblockedEvent publish.");
        }
    }
}
