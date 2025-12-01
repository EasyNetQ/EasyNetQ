using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again : IAsyncLifetime
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

        persistentConnection.When(async x => await x.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>())).Do(_ => throw exception);
        persistentChannel = new PersistentChannel(new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, eventBus);
    }

    private readonly IPersistentChannel persistentChannel;

    [Fact]
    public void Should_throw_timeout_exception()
    {
        Assert.Throws<TaskCanceledException>(() =>
        {
            using var cts = new CancellationTokenSource(1000);
            persistentChannel.InvokeChannelActionAsync(async x => await x.ExchangeDeclareAsync("MyExchange", "direct"), cts.Token).GetAwaiter().GetResult();
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await persistentChannel.DisposeAsync();
    }
}
