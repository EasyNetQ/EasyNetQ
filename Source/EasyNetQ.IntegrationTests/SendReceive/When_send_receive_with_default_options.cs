using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.SendReceive
{
    [Collection("RabbitMQ")]
    public class When_send_receive_with_default_options : IDisposable
    {
        private const int MessagesCount = 10;

        private readonly IBus bus;

        public When_send_receive_with_default_options(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public async Task Test()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = Guid.NewGuid().ToString();
            var messagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);
            using (bus.Receive(queue, x => x.Add<Message>(messagesSink.Receive)))
            {
                await bus.SendBatchAsync(queue, messages, timeoutCts.Token).ConfigureAwait(false);

                await messagesSink.WaitAllReceivedAsync(timeoutCts.Token).ConfigureAwait(false);
                messagesSink.ReceivedMessages.Should().Equal(messages);
            }
        }
    }
}
