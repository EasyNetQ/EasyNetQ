using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.PersistentChannelTests;

public class When_an_action_is_performed_and_channel_reopens
{
    public static IEnumerable<object[]> CloseAndRetryTestCases =>
        new List<object[]>
        {
            new object[]
            {
                new NotSupportedException("Pipelining of requests forbidden")
            },
            new object[]
            {
                new AlreadyClosedException(
                    new ShutdownEventArgs(
                        ShutdownInitiator.Library, AmqpErrorCodes.InternalErrors, "Unexpected Exception"
                    )
                )
            }
        };


    public static IEnumerable<object[]> SoftChannelTestCases =>
        new List<object[]>
        {
            new object[]
            {
                new OperationInterruptedException(
                    new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.AccessRefused, "")
                )
            },

            new object[]
            {
                new OperationInterruptedException(
                    new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.NotFound, "")
                )
            },

            new object[]
            {
                new OperationInterruptedException(
                    new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.PreconditionFailed, "")
                )
            },

            new object[]
            {
                new OperationInterruptedException(
                    new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.ResourceLocked, "")
                )
            }
        };

    [Theory]
    [MemberData(nameof(CloseAndRetryTestCases))]
    public void Should_succeed_after_channel_recreation(Exception exception)
    {
        var persistentConnection = Substitute.For<IPersistentConnection>();
        var brokenChannel = Substitute.For<IChannel, IRecoverable>();
        brokenChannel.When(x => x.ExchangeDeclareAsync("MyExchange", "direct"))
            .Do(_ => throw exception);
        var channel = Substitute.For<IChannel, IRecoverable>();
        persistentConnection.CreateChannelAsync().Returns(_ => brokenChannel, _ => channel);

        using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        persistentChannel.InvokeChannelAction(x => x.ExchangeDeclareAsync("MyExchange", "direct"));

        brokenChannel.Received().ExchangeDeclareAsync("MyExchange", "direct");
        brokenChannel.Received().CloseAsync();
        brokenChannel.Received().Dispose();

        channel.Received().ExchangeDeclareAsync("MyExchange", "direct");
    }

    [Theory]
    [MemberData(nameof(SoftChannelTestCases))]
    public void Should_throw_exception_and_close_channel(Exception exception)
    {
        var persistentConnection = Substitute.For<IPersistentConnection>();
        var brokenChannel = Substitute.For<IChannel, IRecoverable>();
        brokenChannel.When(x => x.ExchangeDeclareAsync("MyExchange", "direct"))
            .Do(_ => throw exception);

        persistentConnection.CreateChannelAsync().Returns(_ => brokenChannel);

        using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        Assert.Throws(
            exception.GetType(),
            () => persistentChannel.InvokeChannelAction(x => x.ExchangeDeclareAsync("MyExchange", "direct"))
        );

        brokenChannel.Received().ExchangeDeclareAsync("MyExchange", "direct");
        brokenChannel.Received().CloseAsync();
        brokenChannel.Received().Dispose();
    }

    [Fact]
    public void Should_succeed_when_broker_reachable()
    {
        var persistentConnection = Substitute.For<IPersistentConnection>();

        var channel = Substitute.For<IChannel, IRecoverable>();
        persistentConnection.CreateChannelAsync()
            .Returns(
                _ => throw new BrokerUnreachableException(new Exception("Oops")),
                _ => channel
            );

        using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        persistentChannel.InvokeChannelAction(x => x.ExchangeDeclareAsync("MyExchange", "direct"));

        channel.Received().ExchangeDeclareAsync("MyExchange", "direct");
    }

    [Fact]
    public void Should_fail_when_auth_is_failed()
    {
        var persistentConnection = Substitute.For<IPersistentConnection>();
        var channel = Substitute.For<IChannel, IRecoverable>();
        persistentConnection.CreateChannelAsync()
            .Returns(
                _ => throw new BrokerUnreachableException(new AuthenticationFailureException("Oops")),
                _ => channel
            );

        using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        Assert.Throws<BrokerUnreachableException>(
            () => persistentChannel.InvokeChannelAction(x => x.ExchangeDeclareAsync("MyExchange", "direct"))
        );

        channel.DidNotReceive().ExchangeDeclareAsync("MyExchange", "direct");
    }
}
