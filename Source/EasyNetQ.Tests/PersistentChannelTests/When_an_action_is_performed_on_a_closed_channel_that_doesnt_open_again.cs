// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Persistent;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace EasyNetQ.Tests.PersistentChannelTests
{
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
            persistentChannel = new PersistentChannel(new PersistentChannelOptions(), persistentConnection, eventBus);
        }

        private readonly IPersistentChannel persistentChannel;

        [Fact]
        public async Task Should_throw_timeout_exception()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                using var cts = new CancellationTokenSource(1000);
                await persistentChannel.InvokeChannelActionAsync(x => x.ExchangeDeclare("MyExchange", "direct"), cts.Token);
            });
        }

        public void Dispose()
        {
            persistentChannel.Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
