using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_exclusive : IDisposable
    {
        public When_publish_and_subscribe_with_exclusive(RabbitMQFixture fixture)
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

            var firstConsumerMessagesSink = new MessagesSink(MessagesCount);
            var secondConsumerMessagesSink = new MessagesSink(0);

            var messages = MessagesFactories.Create(MessagesCount);

            using (
                bus.Subscribe<Message>(
                    Guid.NewGuid().ToString(),
                    firstConsumerMessagesSink.Receive,
                    x => x.AsExclusive()
                )
            )
            {
                // To ensure that ^ subscriber started successfully
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);

                using (
                    bus.Subscribe<Message>(
                        Guid.NewGuid().ToString(),
                        secondConsumerMessagesSink.Receive,
                        x => x.AsExclusive()
                    )
                )
                {
                    await bus.PublishBatchAsync(messages, cts.Token).ConfigureAwait(false);

                    await Task.WhenAll(
                        firstConsumerMessagesSink.WaitAllReceivedAsync(cts.Token),
                        secondConsumerMessagesSink.WaitAllReceivedAsync(cts.Token)
                    ).ConfigureAwait(false);

                    firstConsumerMessagesSink.ReceivedMessages.Should().Equal(messages);
                    secondConsumerMessagesSink.ReceivedMessages.Should().Equal();
                }
            }
        }
    }
}
