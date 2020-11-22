using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_publish_confirms_and_multi_channel_dispatcher : IDisposable
    {
        public When_publish_and_subscribe_with_publish_confirms_and_multi_channel_dispatcher(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus(
                $"host={fixture.Host};prefetchCount=1;publisherConfirms=True",
                c => c.EnableMultiChannelClientCommandDispatcher(2)
            );
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private const int MessagesCount = 20;

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var subscriptionId = Guid.NewGuid().ToString();

            var messagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);

            using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, timeoutCts.Token))
            {
                await bus.PubSub.PublishBatchInParallelAsync(messages, timeoutCts.Token);

                await messagesSink.WaitAllReceivedAsync(timeoutCts.Token);
                messagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
            }
        }
    }
}
