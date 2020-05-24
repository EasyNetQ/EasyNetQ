using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.SendReceive
{
    [Collection("RabbitMQ")]
    public class When_send_receive_multiple_message_types : IDisposable
    {
        private const int MessagesCount = 10;

        private readonly IBus bus;

        public When_send_receive_multiple_message_types(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=5");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public async Task Test()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = Guid.NewGuid().ToString();
            var bunniesSink = new MessagesSink(MessagesCount);
            var rabbitsSink = new MessagesSink(MessagesCount);
            var bunnies = MessagesFactories.Create(MessagesCount, i => new BunnyMessage(i));
            var rabbits = MessagesFactories.Create(MessagesCount, i => new RabbitMessage(i));
            using (
                bus.Receive(
                    queue,
                    x => x.Add<BunnyMessage>(bunniesSink.Receive).Add<RabbitMessage>(rabbitsSink.Receive)
                )
            )
            {
                await bus.SendBatchAsync(queue, bunnies, cts.Token).ConfigureAwait(false);
                await bus.SendBatchAsync(queue, rabbits, cts.Token).ConfigureAwait(false);

                await Task.WhenAll(
                    bunniesSink.WaitAllReceivedAsync(cts.Token),
                    rabbitsSink.WaitAllReceivedAsync(cts.Token)
                ).ConfigureAwait(false);

                bunniesSink.ReceivedMessages.Should().Equal(bunnies);
                rabbitsSink.ReceivedMessages.Should().Equal(rabbits);
            }
        }
    }
}
