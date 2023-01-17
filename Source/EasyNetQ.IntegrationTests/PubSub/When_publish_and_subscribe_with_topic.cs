using EasyNetQ.IntegrationTests.Utils;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_topic : IDisposable
{
    public When_publish_and_subscribe_with_topic(RabbitMQFixture fixture)
    {
        bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=-1");
    }

    public void Dispose()
    {
        bus.Dispose();
    }

    private const int MessagesCount = 10;

    private readonly SelfHostedBus bus;

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
                firstTopicMessages, new PublishConfiguration { Topic = "first" }, cts.Token
            );
            await bus.PubSub.PublishBatchAsync(
                secondTopicMessages, new PublishConfiguration { Topic = "second" }, cts.Token
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
