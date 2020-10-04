using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using EasyNetQ.Scheduling;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Scheduler
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_delay_using_dead_letter_exchange_with_topic : IDisposable
    {
        public When_publish_and_subscribe_with_delay_using_dead_letter_exchange_with_topic(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus(
                $"host={fixture.Host};prefetchCount=1;timeout=5", c => c.EnableDeadLetterExchangeAndMessageTtlScheduler()
            );
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var subscriptionId = Guid.NewGuid().ToString();
            var firstTopicMessagesSink = new MessagesSink(MessagesCount);
            var firstTopicMessages = MessagesFactories.Create(MessagesCount);
            var secondTopicMessagesSink = new MessagesSink(MessagesCount);
            var secondTopicMessages = MessagesFactories.Create(MessagesCount);
            using (bus.Subscribe<Message>(subscriptionId, firstTopicMessagesSink.Receive, x => x.WithTopic("first")))
            using (bus.Subscribe<Message>(subscriptionId, secondTopicMessagesSink.Receive, x => x.WithTopic("second")))
            {
                await Task.WhenAll(
                    bus.FuturePublishBatchAsync(firstTopicMessages, TimeSpan.FromSeconds(5), "first", cts.Token),
                    bus.FuturePublishBatchAsync(secondTopicMessages, TimeSpan.FromSeconds(5), "second", cts.Token)
                ).ConfigureAwait(false);

                await Task.WhenAll(
                    firstTopicMessagesSink.WaitAllReceivedAsync(cts.Token),
                    secondTopicMessagesSink.WaitAllReceivedAsync(cts.Token)
                ).ConfigureAwait(false);

                firstTopicMessagesSink.ReceivedMessages.Should().Equal(firstTopicMessages);
                secondTopicMessagesSink.ReceivedMessages.Should().Equal(secondTopicMessages);
            }
        }
    }
}
