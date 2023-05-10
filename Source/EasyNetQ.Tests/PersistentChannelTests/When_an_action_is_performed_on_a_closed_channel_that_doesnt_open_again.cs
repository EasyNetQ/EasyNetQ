using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Persistent;
using FluentAssertions.Extensions;
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
    public void Should_throw_operation_cancelled_exception()
    {
        Assert.Throws<TaskCanceledException>(() =>
        {
            using var cts = new CancellationTokenSource(1000);
            var timeoutBudget = TimeBudget.Start(20.Seconds());
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"), timeoutBudget, cts.Token);
        });
    }

    [Fact]
    public void Should_throw_timeout_exception()
    {
        Assert.Throws<TimeoutException>(() =>
        {
            var timeoutBudget = TimeBudget.Start(1.Seconds());
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"), timeoutBudget);
        });
    }

    public void Dispose()
    {
        persistentChannel.Dispose();
    }
}
