using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_published_with_mandatory_and_with_publisher_confirms : IDisposable
    {
        private readonly IBus bus;

        public When_published_with_mandatory_and_with_publisher_confirms(RabbitMQFixture rmqFixture)
        {
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
        }

        public void Dispose() => bus.Dispose();

        [Fact]
        public async Task Should_throw_message_returned_exception()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var exchange = await bus.Advanced.ExchangeDeclareAsync(
                Guid.NewGuid().ToString("N"), ExchangeType.Direct, cancellationToken: cts.Token
            );

            await Assert.ThrowsAsync<PublishReturnedException>(
                () => bus.Advanced.PublishAsync(
                    exchange, "#", true, new MessageProperties(), Array.Empty<byte>(), cts.Token
                )
            );
        }
    }
}
