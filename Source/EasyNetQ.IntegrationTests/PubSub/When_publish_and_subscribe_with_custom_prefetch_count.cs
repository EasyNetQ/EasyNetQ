using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_custom_prefetch_count : IDisposable
{
    public When_publish_and_subscribe_with_custom_prefetch_count(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=2;timeout=-1");
    }

    public void Dispose()
    {
        bus.Dispose();
    }

    private const int MessagesCount = 10;

    private readonly IBus bus;

    [Fact]
    public async Task Should_publish_and_consume()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);

        void SlowSyncAction(Message message)
        {
            Thread.Sleep(500);
            messagesSink.Receive(message);
        }

        var started = Stopwatch.GetTimestamp();

        using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, SlowSyncAction))
        {
            await bus.PubSub.PublishBatchAsync(messages, cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);

            Stopwatch.GetElapsedTime(started).Should().BeLessThan(TimeSpan.FromSeconds(3));
        }
    }
}
