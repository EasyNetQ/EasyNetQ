using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again : IDisposable
{
    public When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again()
    {
        using var persistentConnection = Substitute.For<IPersistentConnection>();
        var eventBus = Substitute.For<IEventBus>();
        var shutdownArgs = new ShutdownEventArgs(
            ShutdownInitiator.Peer,
            AmqpErrorCodes.ConnectionClosed,
            "connection closed by peer"
        );
        var exception = new OperationInterruptedException(shutdownArgs);

        persistentConnection.When(async x => await x.CreateChannelAsync()).Do(_ => throw exception);
        persistentChannel = new PersistentChannel(new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, eventBus);
    }

    private readonly IPersistentChannel persistentChannel;

    [Fact]
    public void Should_throw_timeout_exception()
    {
        Assert.Throws<TaskCanceledException>(() =>
        {
            using var cts = new CancellationTokenSource(1000);
            persistentChannel.InvokeChannelAction(async x => await x.ExchangeDeclareAsync("MyExchange", "direct"), cts.Token);
        });
    }

    public virtual void Dispose()
    {
        persistentChannel.Dispose();
    }
}
