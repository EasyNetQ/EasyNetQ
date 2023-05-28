using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Persistent;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again : IDisposable
{
    public When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again()
    {
        var persistentConnection = Substitute.For<IPersistentConnection>();
        var eventBus = Substitute.For<IEventBus>();
        var shutdownArgs = new ShutdownEventArgs(
            ShutdownInitiator.Peer,
            AmqpErrorCodes.ConnectionClosed,
            "connection closed by peer"
        );
        var exception = new OperationInterruptedException(shutdownArgs);

        persistentConnection.When(x => x.CreateModel()).Do(_ => throw exception);
        persistentChannel = new PersistentChannel(new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, eventBus);
    }

    private readonly IPersistentChannel persistentChannel;

    [Fact]
    public async Task Should_timed_out()
    {
        await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await persistentChannel.InvokeChannelActionAsync(
                    x => x.ExchangeDeclare("MyExchange", "direct"),
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None
                );
            }
        );
    }


    [Fact]
    public async Task Should_cancelled()
    {
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

                await persistentChannel.InvokeChannelActionAsync(
                    x => x.ExchangeDeclare("MyExchange", "direct"),
                    TimeoutToken.None,
                    cts.Token
                );
            }
        );
    }

    public void Dispose() => persistentChannel.Dispose();
}
