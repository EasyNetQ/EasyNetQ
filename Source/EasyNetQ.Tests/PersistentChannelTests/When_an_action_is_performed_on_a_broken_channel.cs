// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Persistent;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
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
            var persistentConnection = Substitute.For<IPersistentConnection>();
            var brokenChannel = Substitute.For<IModel, IRecoverable>();
            brokenChannel.When(x => x.ExchangeDeclare("MyExchange", "direct"))
                .Do(_ => throw exception);
            var channel = Substitute.For<IModel, IRecoverable>();
            persistentConnection.CreateModel().Returns(_ => brokenChannel, _ => channel);

            using var persistentChannel = new PersistentChannel(
                new PersistentChannelOptions(), persistentConnection, Substitute.For<IEventBus>()
            );

            await persistentChannel.InvokeChannelActionAsync(x => x.ExchangeDeclare("MyExchange", "direct"));

            brokenChannel.Received().ExchangeDeclare("MyExchange", "direct");
            brokenChannel.Received().Close();
            brokenChannel.Received().Dispose();

            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }

        [Theory]
        [MemberData(nameof(SoftChannelTestCases))]
        public async Task Should_throw_exception_and_close_channel(Exception exception)
        {
            var persistentConnection = Substitute.For<IPersistentConnection>();
            var brokenChannel = Substitute.For<IModel, IRecoverable>();
            brokenChannel.When(x => x.ExchangeDeclare("MyExchange", "direct"))
                .Do(_ => throw exception);

            persistentConnection.CreateModel().Returns(_ => brokenChannel);

            using var persistentChannel = new PersistentChannel(
                new PersistentChannelOptions(), persistentConnection, Substitute.For<IEventBus>()
            );

            await Assert.ThrowsAsync(exception.GetType(), () => persistentChannel.InvokeChannelActionAsync(x => x.ExchangeDeclare("MyExchange", "direct")));

            brokenChannel.Received().ExchangeDeclare("MyExchange", "direct");
            brokenChannel.Received().Close();
            brokenChannel.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
