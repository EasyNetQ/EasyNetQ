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
        public When_send_receive_multiple_message_types(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=-1");
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = Guid.NewGuid().ToString();
            var bunniesSink = new MessagesSink(MessagesCount);
            var rabbitsSink = new MessagesSink(MessagesCount);
            var bunnies = MessagesFactories.Create(MessagesCount, i => new BunnyMessage(i));
            var rabbits = MessagesFactories.Create(MessagesCount, i => new RabbitMessage(i));
            using (
                await bus.SendReceive.ReceiveAsync(
                    queue,
                    x => x.Add<BunnyMessage>(bunniesSink.Receive).Add<RabbitMessage>(rabbitsSink.Receive),
                    cts.Token
                )
            )
            {
                await bus.SendReceive.SendBatchAsync(queue, bunnies, cts.Token);
                await bus.SendReceive.SendBatchAsync(queue, rabbits, cts.Token);

                await Task.WhenAll(
                    bunniesSink.WaitAllReceivedAsync(cts.Token),
                    rabbitsSink.WaitAllReceivedAsync(cts.Token)
                );

                bunniesSink.ReceivedMessages.Should().Equal(bunnies);
                rabbitsSink.ReceivedMessages.Should().Equal(rabbits);
            }
        }
    }
}
