using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Scheduler;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_delay_using_dead_letter_exchange_with_topic : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_delay_using_dead_letter_exchange_with_topic(RabbitMQFixture fixture)
    {
        var connectionString = $"host={fixture.Host};prefetchCount=1;timeout=5";
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ(connectionString);
        serviceCollection.EnableDelayedExchangeScheduler();

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var firstTopicMessagesSink = new MessagesSink(MessagesCount);
        var firstTopicMessages = MessagesFactories.Create(MessagesCount);
        var secondTopicMessagesSink = new MessagesSink(MessagesCount);
        var secondTopicMessages = MessagesFactories.Create(MessagesCount, MessagesCount);
        using (await bus.PubSub.SubscribeAsync<Message>(Guid.NewGuid().ToString(), firstTopicMessagesSink.Receive, x => x.WithTopic("first")))
        using (await bus.PubSub.SubscribeAsync<Message>(Guid.NewGuid().ToString(), secondTopicMessagesSink.Receive, x => x.WithTopic("second")))
        {
            await Task.WhenAll(
                bus.Scheduler.FuturePublishBatchAsync(firstTopicMessages, TimeSpan.FromSeconds(5), "first", cts.Token),
                bus.Scheduler.FuturePublishBatchAsync(secondTopicMessages, TimeSpan.FromSeconds(5), "second", cts.Token)
            );

            await Task.WhenAll(
                firstTopicMessagesSink.WaitAllReceivedAsync(cts.Token),
                secondTopicMessagesSink.WaitAllReceivedAsync(cts.Token)
            );

            firstTopicMessagesSink.ReceivedMessages.Should().Equal(firstTopicMessages);
            secondTopicMessagesSink.ReceivedMessages.Should().Equal(secondTopicMessages);
        }
    }
}
