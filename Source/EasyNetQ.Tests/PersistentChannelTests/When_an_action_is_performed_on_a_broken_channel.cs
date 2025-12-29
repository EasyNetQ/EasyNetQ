using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
    public async Task Should_succeed_after_channel_recreation(Exception exception)
    {
        using var persistentConnection = Substitute.For<IPersistentConnection>();
        var brokenChannel = Substitute.For<IChannel, IRecoverable>();
        brokenChannel.When(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct))
            .Do(_ => throw exception);
        var channel = Substitute.For<IChannel, IRecoverable>();
#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), default).Returns(_ => brokenChannel, _ => channel);
#pragma warning restore IDISP004

        await using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        await persistentChannel.InvokeChannelActionAsync(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct));

        await brokenChannel.Received().ExchangeDeclareAsync("MyExchange", ExchangeType.Direct);
        await brokenChannel.Received().CloseAsync();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        brokenChannel.Received().DisposeAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        await channel.Received().ExchangeDeclareAsync("MyExchange", ExchangeType.Direct);
    }

    [Theory]
    [MemberData(nameof(SoftChannelTestCases))]
    public async Task Should_throw_exception_and_close_channel(Exception exception)
    {
        using var persistentConnection = Substitute.For<IPersistentConnection>();
        var brokenChannel = Substitute.For<IChannel, IRecoverable>();
        brokenChannel.When(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct))
            .Do(_ => throw exception);

#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), default).Returns(_ => brokenChannel);
#pragma warning restore IDISP004

        await using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );
        await Assert.ThrowsAsync(
            exception.GetType(),
            async () => await persistentChannel.InvokeChannelActionAsync(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct))
        );

        await brokenChannel.Received().ExchangeDeclareAsync("MyExchange", ExchangeType.Direct);
        await brokenChannel.Received().CloseAsync();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        brokenChannel.Received().DisposeAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    [Fact]
    public async Task Should_succeed_when_broker_reachable()
    {
        using var persistentConnection = Substitute.For<IPersistentConnection>();

        var channel = Substitute.For<IChannel, IRecoverable>();
#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), default)
#pragma warning restore IDISP004
            .Returns(
                _ => throw new BrokerUnreachableException(new Exception("Oops")),
                _ => channel
            );

        await using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        await persistentChannel.InvokeChannelActionAsync(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct));

        await channel.Received().ExchangeDeclareAsync("MyExchange", ExchangeType.Direct);
    }

    [Fact]
    public async Task Should_fail_when_auth_is_failed()
    {
        using var persistentConnection = Substitute.For<IPersistentConnection>();
        var channel = Substitute.For<IChannel, IRecoverable>();
#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), default)
#pragma warning restore IDISP004
            .Returns(
                _ => throw new BrokerUnreachableException(new AuthenticationFailureException("Oops")),
                _ => channel
            );

        await using var persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(), Substitute.For<ILogger<PersistentChannel>>(), persistentConnection, Substitute.For<IEventBus>()
        );

        await Assert.ThrowsAsync<BrokerUnreachableException>(
            async () => await persistentChannel.InvokeChannelActionAsync(async x => await x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct))
        );

        await channel.DidNotReceive().ExchangeDeclareAsync("MyExchange", ExchangeType.Direct);
    }
}
