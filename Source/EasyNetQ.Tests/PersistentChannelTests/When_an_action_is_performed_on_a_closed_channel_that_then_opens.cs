// ReSharper disable InconsistentNaming

using System.Threading;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using Xunit;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_an_action_is_performed_on_a_closed_channel_that_then_opens
    {
        private IPersistentChannel persistentChannel;
        private IPersistentConnection persistentConnection;
        private IModel channel;

        public When_an_action_is_performed_on_a_closed_channel_that_then_opens()
        {
            persistentConnection = Substitute.For<IPersistentConnection>();
            channel = Substitute.For<IModel>();
            var eventBus = new EventBus();
            var configuration = new ConnectionConfiguration();

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer, 
                AmqpException.ConnectionClosed,
                "connection closed by peer");
            var exception = new OperationInterruptedException(shutdownArgs);

            persistentConnection.CreateModel().Returns(x => { throw exception; },
                                                       x => channel,
                                                       x => channel); 
                
            persistentChannel = new PersistentChannel(persistentConnection, configuration, eventBus);

            new Timer(_ => eventBus.Publish(new ConnectionCreatedEvent()), null, 10, Timeout.Infinite);

            persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"));
        }

        [Fact]
        public void Should_run_action_on_channel()
        {
            Thread.Sleep(100);
            channel.Received().ExchangeDeclare("MyExchange", "direct");
        }
    }
}

// ReSharper restore InconsistentNaming