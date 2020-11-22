using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using Xunit;
using ExchangeType = EasyNetQ.Topology.ExchangeType;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_declare_a_queue : IDisposable
    {
        public When_declare_a_queue(RabbitMQFixture rmqFixture)
        {
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");
        }

        public void Dispose() => bus.Dispose();

        private readonly IBus bus;

        [Fact]
        public async Task Should_declare_queue_with_different_modes_and_types()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queuesModes = new [] {QueueMode.Default, QueueMode.Lazy};
            foreach (var queueMode in queuesModes)
            {
                await bus.Advanced.QueueDeclareAsync(
                    Guid.NewGuid().ToString("N"), c => c.WithQueueMode(queueMode), cts.Token
                );
            }

            var queuesTypes = new [] {QueueType.Classic, QueueType.Quorum};
            foreach (var queueType in queuesTypes)
            {
                await bus.Advanced.QueueDeclareAsync(
                    Guid.NewGuid().ToString("N"), c => c.WithQueueType(queueType), cts.Token
                );
            }
        }
    }
}
