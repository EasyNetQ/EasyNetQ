using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_topic : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_topic(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1");

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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var firstTopicMessagesSink = new MessagesSink(MessagesCount);
        var secondTopicMessagesSink = new MessagesSink(MessagesCount);

        var firstTopicMessages = MessagesFactories.Create(MessagesCount);
        var secondTopicMessages = MessagesFactories.Create(MessagesCount, MessagesCount);

        using (
            await bus.PubSub.SubscribeAsync<Message>(
                Guid.NewGuid().ToString(),
                firstTopicMessagesSink.Receive,
                x => x.WithTopic("first"),
                cts.Token
            )
        )
        using (
            await bus.PubSub.SubscribeAsync<Message>(
                Guid.NewGuid().ToString(),
                secondTopicMessagesSink.Receive,
                x => x.WithTopic("second"),
                cts.Token
            )
        )
        {
            await bus.PubSub.PublishBatchAsync(
                firstTopicMessages, x => x.WithTopic("first"), cts.Token
            );
            await bus.PubSub.PublishBatchAsync(
                secondTopicMessages, x => x.WithTopic("second"), cts.Token
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
