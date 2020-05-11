using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.SendReceive
{
    [Collection("RabbitMQ")]
    public class When_send_receive_with_publish_confirms : IDisposable
    {
        public When_send_receive_with_publish_confirms(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;publisherConfirms=True");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private const int MessagesCount = 10;

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = Guid.NewGuid().ToString();
            var messagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);
            using (
                await bus.SendReceive.ReceiveAsync(queue, x => x.Add<Message>(messagesSink.Receive), timeoutCts.Token)
            )
            {
                await bus.SendReceive.SendBatchAsync(queue, messages, timeoutCts.Token).ConfigureAwait(false);

                await messagesSink.WaitAllReceivedAsync(timeoutCts.Token).ConfigureAwait(false);
                messagesSink.ReceivedMessages.Should().Equal(messages);
            }
        }
    }
}
