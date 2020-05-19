// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Producer;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
    public class When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again: IDisposable
    {
        public When_an_action_is_performed_on_a_closed_channel_that_doesnt_open_again()
        {
            var persistentConnection = Substitute.For<IPersistentConnection>();
            var eventBus = Substitute.For<IEventBus>();

            var configuration = new ConnectionConfiguration
            {
                Timeout = TimeSpan.FromSeconds(1)
            };

            var shutdownArgs = new ShutdownEventArgs(
                ShutdownInitiator.Peer,
                AmqpException.ConnectionClosed,
                "connection closed by peer");
            var exception = new OperationInterruptedException(shutdownArgs);

            persistentConnection.When(x => x.CreateModel()).Do(x => throw exception);

            persistentChannel = new PersistentChannel(persistentConnection, configuration, eventBus);
        }

        private readonly IPersistentChannel persistentChannel;

        [Fact]
        public void Should_throw_timeout_exception()
        {
            Assert.Throws<TaskCanceledException>(() =>
            {
                using var cts = new CancellationTokenSource(1000);
                persistentChannel.InvokeChannelAction(x => x.ExchangeDeclare("MyExchange", "direct"), cts.Token);
            });
        }

        public void Dispose()
        {
            persistentChannel.Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
