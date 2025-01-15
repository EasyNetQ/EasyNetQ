using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_default_options : IDisposable
{
    private readonly RabbitMQFixture rmqFixture;

    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_default_options(RabbitMQFixture rmqFixture)
    {
        this.rmqFixture = rmqFixture;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Should_publish_and_consume()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);

        using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive))
        {
            await bus.PubSub.PublishBatchAsync(messages, cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().Equal(messages);
        }
    }

    [Fact]
    public async Task Should_publish_and_consume_with_multiple_subscription_ids()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var firstConsumerMessagesSink = new MessagesSink(MessagesCount);
        var secondConsumerMessagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);

        using (
            await bus.PubSub.SubscribeAsync<Message>(
                Guid.NewGuid().ToString(), firstConsumerMessagesSink.Receive, cts.Token
            )
        )
        using (
            await bus.PubSub.SubscribeAsync<Message>(
                Guid.NewGuid().ToString(), secondConsumerMessagesSink.Receive, cts.Token
            )
        )
        {
            await bus.PubSub.PublishBatchAsync(messages, cts.Token);

            await Task.WhenAll(
                firstConsumerMessagesSink.WaitAllReceivedAsync(cts.Token),
                secondConsumerMessagesSink.WaitAllReceivedAsync(cts.Token)
            );

            firstConsumerMessagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
            secondConsumerMessagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
        }
    }

    [Fact]
    public async Task Should_publish_and_consume_with_same_subscription_ids()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);

        using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, cts.Token))
        using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, cts.Token))
        using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, cts.Token))
        {
            await bus.PubSub.PublishBatchAsync(messages, cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
        }
    }

    [Fact]
    public async Task Should_survive_restart()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(2);
        using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, cts.Token))
        {
            var message = new Message(0);
            await bus.PubSub.PublishAsync(message, cts.Token);
            await rmqFixture.ManagementClient.KillAllConnectionsAsync(cts.Token);
            await bus.PubSub.PublishAsync(message, cts.Token);
            await messagesSink.WaitAllReceivedAsync(cts.Token);
        }
    }
}
