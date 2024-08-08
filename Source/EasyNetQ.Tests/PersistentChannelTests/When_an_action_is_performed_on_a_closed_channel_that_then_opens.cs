using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_performed_on_a_closed_channel_that_then_opens
{
    public When_an_action_is_performed_on_a_closed_channel_that_then_opens()
    {
        var persistentConnection = Substitute.For<IPersistentConnection>();
        channel = Substitute.For<IChannel, IRecoverable>();
        var eventBus = new EventBus(Substitute.For<ILogger<EventBus>>());

        var shutdownArgs = new ShutdownEventArgs(
            ShutdownInitiator.Peer,
            AmqpErrorCodes.ConnectionClosed,
            "connection closed by peer"
        );
        var exception = new OperationInterruptedException(shutdownArgs);

        persistentConnection.CreateChannelAsync().Returns(
            _ => throw exception, _ => channel, _ => channel
        );

        var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, eventBus
        );
        persistentChannel.InvokeChannelAction(x => x.ExchangeDeclareAsync("MyExchange", "direct"));
    }

    private readonly IChannel channel;

    [Fact]
    public async Task Should_run_action_on_channel()
    {
        await channel.Received().ExchangeDeclareAsync("MyExchange", "direct");
    }
}
