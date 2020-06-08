// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using EasyNetQ.Producer;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_an_action_is_performed_and_channel_reopens
    {
        public static IEnumerable<object[]> PipeliningForbiddenTestCases =>
            new List<object[]>
            {
                new object[]
                {
                    new NotSupportedException("Pipelining of requests forbidden")
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
        [MemberData(nameof(PipeliningForbiddenTestCases))]
        public void Should_succeed_after_channel_recreation(Exception exception)
        {
            var persistentConnection = Substitute.For<IPersistentConnection>();
            var brokenChannel = Substitute.For<IModel, IRecoverable>();
            brokenChannel.When(x => x.ExchangeDeclare("MyExchange", "direct"))
                .Do(x => throw exception);
            var channel = Substitute.For<IModel, IRecoverable>();
            persistentConnection.CreateModel().Returns(x => brokenChannel, x => channel);

            using var persistentChannel = new PersistentChannel(
                new PersistentChannelOptions(), persistentConnection, Substitute.For<IEventBus>()
            );

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));

            brokenChannel.Received().ExchangeDeclare("MyExchange", "direct");
            brokenChannel.Received().Close();
            brokenChannel.Received().Dispose();

            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }

        [Theory]
        [MemberData(nameof(SoftChannelTestCases))]
        public void Should_throw_exception_and_close_channel(Exception exception)
        {
            var persistentConnection = Substitute.For<IPersistentConnection>();
            var brokenChannel = Substitute.For<IModel, IRecoverable>();
            brokenChannel.When(x => x.ExchangeDeclare("MyExchange", "direct"))
                .Do(x => throw exception);

            persistentConnection.CreateModel().Returns(x => brokenChannel);

            using var persistentChannel = new PersistentChannel(
                new PersistentChannelOptions(), persistentConnection, Substitute.For<IEventBus>()
            );

            Assert.Throws(
                exception.GetType(),
                () => persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"))
            );

            brokenChannel.Received().ExchangeDeclare("MyExchange", "direct");
            brokenChannel.Received().Close();
            brokenChannel.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
