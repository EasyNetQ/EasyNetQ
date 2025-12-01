using System.Threading.Tasks;
using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

public class AdvancedBusEventHandlersTests : IDisposable
{
    public AdvancedBusEventHandlersTests()
    {
        var advancedBusEventHandlers = new AdvancedBusEventHandlers(
            (_, e) =>
            {
                connectedCalled = true;
                connectedEventArgs = e;
            },
            (_, e) =>
            {
                disconnectedCalled = true;
                disconnectedEventArgs = e;
            },
            (_, e) =>
            {
                blockedCalled = true;
                blockedEventArgs = e;
            },
            (_, _) => unBlockedCalled = true,
            (_, e) =>
            {
                messageReturnedCalled = true;
                messageReturnedEventArgs = e;
            }
        );

        eventBus = new EventBus(Substitute.For<ILogger<EventBus>>());

        advancedBus = new RabbitAdvancedBus(
            Substitute.For<ILogger<RabbitAdvancedBus>>(),
            Substitute.For<IProducerConnection>(),
            Substitute.For<IConsumerConnection>(),
            Substitute.For<IConsumerFactory>(),
            Substitute.For<IPersistentChannelDispatcher>(),
            Substitute.For<IPublishConfirmationListener>(),
            eventBus,
            Substitute.For<IHandlerCollectionFactory>(),
            Substitute.For<ConnectionConfiguration>(),
            new ProducePipelineBuilder(),
            new ConsumePipelineBuilder(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IMessageSerializationStrategy>(),
            Substitute.For<IPullingConsumerFactory>(),
            advancedBusEventHandlers
        );
    }

    public virtual void Dispose()
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
    private readonly RabbitAdvancedBus advancedBus;
    private ConnectedEventArgs connectedEventArgs;
    private DisconnectedEventArgs disconnectedEventArgs;

    [Fact]
    public async Task AdvancedBusEventHandlers_Blocked_handler_is_called()
    {
        var @event = new ConnectionBlockedEvent(PersistentConnectionType.Producer, "a random reason");

        await eventBus.PublishAsync(@event);
        blockedCalled.Should().BeTrue();
        blockedEventArgs.Should().NotBeNull();
        blockedEventArgs.Reason.Should().Be(@event.Reason);
    }

    [Fact]
    public async Task AdvancedBusEventHandlers_Connected_handler_is_called_when_connection_recovered()
    {
        await eventBus.PublishAsync(new ConnectionRecoveredEvent(PersistentConnectionType.Producer, new AmqpTcpEndpoint()));
        connectedCalled.Should().BeTrue();
        connectedEventArgs.Hostname.Should().Be("localhost");
        connectedEventArgs.Port.Should().Be(5672);
    }

    [Fact]
    public async Task AdvancedBusEventHandlers_Connected_handler_is_called_when_connection_created()
    {
        await eventBus.PublishAsync(new ConnectionCreatedEvent(PersistentConnectionType.Producer, new AmqpTcpEndpoint()));
        connectedCalled.Should().BeTrue();
        connectedEventArgs.Hostname.Should().Be("localhost");
        connectedEventArgs.Port.Should().Be(5672);
    }

    [Fact]
    public async Task AdvancedBusEventHandlers_Disconnected_handler_is_called()
    {
        var @event = new ConnectionDisconnectedEvent(
            PersistentConnectionType.Producer, new AmqpTcpEndpoint(), "a random reason"
        );
        await eventBus.PublishAsync(@event);
        disconnectedCalled.Should().BeTrue();
        disconnectedEventArgs.Should().NotBeNull();
        disconnectedEventArgs.Hostname.Should().Be("localhost");
        disconnectedEventArgs.Port.Should().Be(5672);
        disconnectedEventArgs.Reason.Should().Be("a random reason");
    }

    [Fact]
    public async Task AdvancedBusEventHandlers_MessageReturned_handler_is_called()
    {
        var @event = new ReturnedMessageEvent(
            Substitute.For<IChannel>(),
            Array.Empty<byte>(),
            MessageProperties.Empty,
            new MessageReturnedInfo("my.exchange", "routing.key", "reason")
        );

        await eventBus.PublishAsync(@event);
        messageReturnedCalled.Should().BeTrue();
        messageReturnedEventArgs.Should().NotBeNull();
        messageReturnedEventArgs.MessageBody.ToArray().Should().Equal(@event.Body.ToArray());
        messageReturnedEventArgs.MessageProperties.Should().Be(@event.Properties);
        messageReturnedEventArgs.MessageReturnedInfo.Should().Be(@event.Info);
    }

    [Fact]
    public async Task AdvancedBusEventHandlers_Unblocked_handler_is_called()
    {
        await eventBus.PublishAsync(new ConnectionUnblockedEvent(PersistentConnectionType.Producer));
        unBlockedCalled.Should().BeTrue();
    }
}
