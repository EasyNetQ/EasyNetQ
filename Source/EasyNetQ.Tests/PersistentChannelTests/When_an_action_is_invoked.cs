using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_invoked : IAsyncLifetime
{
    private readonly IPersistentChannel persistentChannel;
    private readonly IPersistentConnection persistentConnection;
    private readonly IChannel channel;

    public When_an_action_is_invoked()
    {
        persistentConnection = Substitute.For<IPersistentConnection>();
        channel = Substitute.For<IChannel, IRecoverable>();

#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), default).Returns(channel);
#pragma warning restore IDISP004

        persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(),
            Substitute.For<ILogger<PersistentChannel>>(),
            persistentConnection,
            Substitute.For<IEventBus>()
        );
    }

    public async ValueTask InitializeAsync()
    {
        await persistentChannel.InvokeChannelActionAsync(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct));
    }

    [Fact]
    public async Task Should_open_a_channel()
    {
        await persistentConnection.Received().CreateChannelAsync(Arg.Any<CreateChannelOptions>(), CancellationToken.None);
    }

    [Fact]
    public async Task Should_run_action_on_channel()
    {
        await channel.Received().ExchangeDeclareAsync("MyExchange", ExchangeType.Direct, cancellationToken: CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await persistentChannel.DisposeAsync();
    }
}
