// ReSharper disable InconsistentNaming

using EasyNetQ.Logging;
using EasyNetQ.Persistent;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_an_action_is_performed_on_a_closed_channel_that_then_opens
    {
        public When_an_action_is_performed_on_a_closed_channel_that_then_opens()
        {
            var persistentConnection = Substitute.For<IPersistentConnection>();
            channel = Substitute.For<IModel, IRecoverable>();
            var eventBus = new EventBus(Substitute.For<ILogger<EventBus>>());

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer,
                AmqpErrorCodes.ConnectionClosed,
                "connection closed by peer"
            );
            var exception = new OperationInterruptedException(shutdownArgs);

            persistentConnection.CreateModel().Returns(
                _ => throw exception, _ => channel, _ => channel
            );

            var persistentChannel = new PersistentChannel(
                new PersistentChannelOptions(), persistentConnection, eventBus
            );
            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        private readonly IModel channel;

        [Fact]
        public void Should_run_action_on_channel()
        {
            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }
    }
}

// ReSharper restore InconsistentNaming
