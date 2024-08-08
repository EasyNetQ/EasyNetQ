using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_invoked : IDisposable
{
    public When_an_action_is_invoked()
    {
        persistentConnection = Substitute.For<IPersistentConnection>();
        channel = Substitute.For<IChannel, IRecoverable>();

        persistentConnection.CreateChannelAsync().Returns(channel);

        persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        persistentChannel.InvokeChannelAction(async x => await x.ExchangeDeclareAsync("MyExchange", "direct"));
    }

    private readonly IPersistentChannel persistentChannel;
    private readonly IPersistentConnection persistentConnection;
    private readonly IChannel channel;

    [Fact]
    public async Task Should_open_a_channel()
    {
        await persistentConnection.Received().CreateChannelAsync();
    }

    [Fact]
    public async Task Should_run_action_on_channel()
    {
        await channel.Received().ExchangeDeclareAsync("MyExchange", "direct");
    }

    public void Dispose()
    {
        persistentChannel.Dispose();
    }
}
