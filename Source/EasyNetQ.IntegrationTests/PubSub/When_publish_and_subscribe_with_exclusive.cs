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
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1");
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
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var firstConsumerMessagesSink = new MessagesSink(MessagesCount);
            var secondConsumerMessagesSink = new MessagesSink(0);

            var messages = MessagesFactories.Create(MessagesCount);

            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    Guid.NewGuid().ToString(),
                    firstConsumerMessagesSink.Receive,
                    x => x.AsExclusive(),
                    timeoutCts.Token
                )
            )
            {
                // To ensure that ^ subscriber started successfully
                await Task.Delay(TimeSpan.FromSeconds(1), timeoutCts.Token).ConfigureAwait(false);

                using (
                    await bus.PubSub.SubscribeAsync<Message>(
                        Guid.NewGuid().ToString(),
                        secondConsumerMessagesSink.Receive,
                        x => x.AsExclusive(),
                        timeoutCts.Token
                    )
                )
                {
                    await bus.PubSub.PublishBatchAsync(messages, timeoutCts.Token).ConfigureAwait(false);

                    await Task.WhenAll(
                        firstConsumerMessagesSink.WaitAllReceivedAsync(timeoutCts.Token),
                        secondConsumerMessagesSink.WaitAllReceivedAsync(timeoutCts.Token)
                    ).ConfigureAwait(false);

                    firstConsumerMessagesSink.ReceivedMessages.Should().Equal(messages);
                    secondConsumerMessagesSink.ReceivedMessages.Should().Equal();
                }
            }
        }
    }
}
