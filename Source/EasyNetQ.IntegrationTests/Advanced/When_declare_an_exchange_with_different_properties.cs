using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using Xunit;
using ExchangeType = EasyNetQ.Topology.ExchangeType;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_declare_an_exchange_with_different_properties : IDisposable
    {
        public When_declare_an_exchange_with_different_properties(RabbitMQFixture rmqFixture)
        {
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
        }

        public void Dispose() => bus.Dispose();

        private readonly IBus bus;

        [Fact]
        public async Task Should_not_affect_correct_declares()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await bus.Advanced.ExchangeDeclareAsync("a", ExchangeType.Topic, cancellationToken: cts.Token);
            await Assert.ThrowsAsync<OperationInterruptedException>(
                () => bus.Advanced.ExchangeDeclareAsync("a", ExchangeType.Direct, cancellationToken: cts.Token)
            );
            await bus.Advanced.ExchangeDeclareAsync("a", ExchangeType.Topic, cancellationToken: cts.Token);
        }
    }
}
