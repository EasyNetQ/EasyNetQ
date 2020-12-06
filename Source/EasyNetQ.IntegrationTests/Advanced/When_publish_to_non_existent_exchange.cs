using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using RabbitMQ.Client.Exceptions;
using Xunit;
using ExchangeType = EasyNetQ.Topology.ExchangeType;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_publish_to_non_existent_exchange : IDisposable
    {
        public When_publish_to_non_existent_exchange(RabbitMQFixture rmqFixture)
        {
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
        }

        public void Dispose() => bus.Dispose();

        private readonly IBus bus;

        [Fact]
        public async Task Should_not_affect_publish_to_existent_exchange()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await bus.Advanced.ExchangeDeclareAsync("existent", ExchangeType.Topic, cancellationToken: cts.Token);
            await Assert.ThrowsAsync<AlreadyClosedException>(() =>
                bus.Advanced.PublishAsync(
                    new Exchange("non-existent"), "#", false, new MessageProperties(), Array.Empty<byte>(), cts.Token
                )
            );
            await bus.Advanced.PublishAsync(
                new Exchange("existent"), "#", false, new MessageProperties(), Array.Empty<byte>(), cts.Token
            );
        }
    }
}
