using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NSubstitute;

namespace EasyNetQ.Tests
{
    [TestFixture]
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

        [SetUp]
        public void SetUp()
        {
            advancedBusEventHandlers = new AdvancedBusEventHandlers(
                connected: (s, e) => connectedCalled = true,
                disconnected: (s, e) => disconnectedCalled = true,
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
            connectionFactory.CreateConnection().Returns(Substitute.For<IConnection>());
            connectionFactory.CurrentHost.Returns(new HostConfiguration());
            connectionFactory.Configuration.Returns(new ConnectionConfiguration());

            eventBus = new EventBus();

            var advancedBus = new RabbitAdvancedBus(
                connectionFactory,
                Substitute.For<IConsumerFactory>(),
                Substitute.For<IEasyNetQLogger>(),
                Substitute.For<IClientCommandDispatcherFactory>(),
                Substitute.For<IPublishConfirmationListener>(),
                eventBus,
                Substitute.For<IHandlerCollectionFactory>(),
                Substitute.For<IContainer>(),
                Substitute.For<ConnectionConfiguration>(),
                Substitute.For<IProduceConsumeInterceptor>(),
                Substitute.For<IMessageSerializationStrategy>(),
                Substitute.For<IConventions>(),
                advancedBusEventHandlers);
        }

        [Test]
        public void AdvancedBusEventHandlers_Connected_handler_is_called_when_advancedbus_connects_for_the_first_time()
        {
            Assert.IsTrue(connectedCalled, "The AdvancedBusEventHandlers Connected event handler wasn't called during RabbitAdvancedBus instantiation.");
        }

        [Test]
        public void AdvancedBusEventHandlers_Connected_handler_is_called()
        {
            eventBus.Publish(new ConnectionCreatedEvent());
            Assert.IsTrue(connectedCalled, "The AdvancedBusEventHandlers Connected event handler wasn't called after a ConnectionCreatedEvent publish.");
        }

        [Test]
        public void AdvancedBusEventHandlers_Disconnected_handler_is_called()
        {
            eventBus.Publish(new ConnectionDisconnectedEvent());
            Assert.IsTrue(disconnectedCalled, "The AdvancedBusEventHandlers Disconnected event handler wasn't called after a ConnectionDisconnectedEvent publish.");
        }

        [Test]
        public void AdvancedBusEventHandlers_Blocked_handler_is_called()
        {
            var connectionBlockedEvent = new ConnectionBlockedEvent("a random reason");

            eventBus.Publish(connectionBlockedEvent);
            Assert.IsTrue(blockedCalled, "The AdvancedBusEventHandlers Blocked event handler wasn't called after a ConnectionBlockedEvent publish.");
            Assert.IsNotNull(connectionBlockedEventArgs, "The AdvancedBusEventHandlers Blocked event handler received a null ConnectionBlockedEventArgs");
            Assert.IsTrue(connectionBlockedEvent.Reason == connectionBlockedEventArgs.Reason, "The published ConnectionBlockedEvent Reason isn't the same object than the one received in AdvancedBusEventHandlers Blocked ConnectionBlockedEventArgs.");
        }

        [Test]
        public void AdvancedBusEventHandlers_Unblocked_handler_is_called()
        {
            eventBus.Publish(new ConnectionUnblockedEvent());
            Assert.IsTrue(unBlockedCalled, "The AdvancedBusEventHandlers Unblocked event handler wasn't called after a ConnectionUnblockedEvent publish.");
        }

        [Test]
        public void AdvancedBusEventHandlers_MessageReturned_handler_is_called()
        {
            var returnedMessageEvent = new ReturnedMessageEvent(new byte[0], new MessageProperties(), new MessageReturnedInfo("my.exchange", "routing.key", "reason"));

            eventBus.Publish(returnedMessageEvent);
            Assert.IsTrue(messageReturnedCalled, "The AdvancedBusEventHandlers MessageReturned event handler wasn't called after a ReturnedMessageEvent publish.");
            Assert.IsNotNull(messageReturnedEventArgs, "The AdvancedBusEventHandlers MessageReturned event handler received a null MessageReturnedEventArgs.");
            Assert.IsTrue(returnedMessageEvent.Body == messageReturnedEventArgs.MessageBody, "The published ReturnedMessageEvent Body isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
            Assert.IsTrue(returnedMessageEvent.Properties == messageReturnedEventArgs.MessageProperties, "The published ReturnedMessageEvent Properties isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
            Assert.IsTrue(returnedMessageEvent.Info == messageReturnedEventArgs.MessageReturnedInfo, "The published ReturnedMessageEvent Info isn't the same object than the one received in AdvancedBusEventHandlers MessageReturned MessageReturnedEventArgs.");
        }
    }
}