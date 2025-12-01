using System.Diagnostics;
using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_custom_prefetch_count : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_custom_prefetch_count(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=2;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public void Dispose()
    {
        serviceProvider?.Dispose();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Should_publish_and_consume()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(2 * MessagesCount);
        var messages = MessagesFactories.Create(2 * MessagesCount);

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

            Stopwatch.GetElapsedTime(started).Should().BeLessThan(TimeSpan.FromSeconds(7.5));
        }
    }
}
