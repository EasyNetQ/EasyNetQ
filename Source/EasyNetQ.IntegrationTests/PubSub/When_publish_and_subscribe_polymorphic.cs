using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_polymorphic : IDisposable
    {
        public When_publish_and_subscribe_polymorphic(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=5");
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

            var subscriptionId = Guid.NewGuid().ToString();

            var bunniesSink = new MessagesSink(MessagesCount);
            var rabbitsSink = new MessagesSink(MessagesCount);
            var bunnies = MessagesFactories.Create(MessagesCount, i => new BunnyMessage(i));
            var rabbits = MessagesFactories.Create(MessagesCount, MessagesCount, i => new RabbitMessage(i));

            using (bus.Subscribe<Message>(subscriptionId, x =>
            {
                switch (x)
                {
                    case BunnyMessage _:
                        bunniesSink.Receive(x);
                        break;
                    case RabbitMessage _:
                        rabbitsSink.Receive(x);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(x), x, null);
                }
            }))
            {
                await bus.PublishBatchAsync(bunnies.Concat(rabbits), cts.Token)
                    .ConfigureAwait(false);

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
