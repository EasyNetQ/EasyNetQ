using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_connected_event_raised : IDisposable
    {
        public When_connected_event_raised(RabbitMQFixture rmqFixture)
        {
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
        }

        public void Dispose() => bus.Dispose();

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var mre = new ManualResetEventSlim(false);
            bus.Advanced.Connected += (sender, args) => mre.Set();

            await bus.Advanced.ExchangeDeclareAsync(
                Guid.NewGuid().ToString("N"), c => c.WithType(ExchangeType.Topic), cts.Token
            );

            mre.Wait(cts.Token);
        }
    }
}
