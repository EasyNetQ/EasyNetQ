using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_invoked : IDisposable
{
    private readonly IPersistentChannel persistentChannel;
    private readonly IPersistentConnection persistentConnection;
    private readonly IChannel channel;

    public When_an_action_is_invoked()
    {
        persistentConnection = Substitute.For<IPersistentConnection>();
        channel = Substitute.For<IChannel, IRecoverable>();

#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync().Returns(channel);
#pragma warning restore IDISP004

        persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(),
            Substitute.For<ILogger<PersistentChannel>>(),
            persistentConnection,
            Substitute.For<IEventBus>()
        );

        persistentChannel.InvokeChannelAction(async x => await x.ExchangeDeclareAsync("MyExchange", "direct"));
    }

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

    public virtual void Dispose()
    {
        persistentChannel.Dispose();
    }
}
