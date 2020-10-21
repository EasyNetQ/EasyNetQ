using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_connecting_to_non_existed_vhost : IDisposable
    {
        public When_connecting_to_non_existed_vhost(RabbitMQFixture rmqFixture)
        {
            Console.WriteLine(rmqFixture.Host);
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
        }

        public void Dispose() => bus.Dispose();

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));

            IExchange e;
            try
            {
                e = await bus.Advanced.ExchangeDeclareAsync("this_exchange_will_never_be_Created", c => { }, cts.Token);
                Assert.True(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Canceled");
            }
        }
    }
}
