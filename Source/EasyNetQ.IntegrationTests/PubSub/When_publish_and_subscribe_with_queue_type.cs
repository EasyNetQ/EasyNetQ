using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_queue_type : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_queue_type(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Should_publish_and_consume()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = CreateMessages(MessagesCount);

        await using (await bus.PubSub.SubscribeAsync<QuorumQueueMessage>(subscriptionId, messagesSink.Receive, cancellationToken: cts.Token))
        {
            await bus.PubSub.PublishBatchAsync(messages, cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().Equal(messages);
        }
    }

    private static List<QuorumQueueMessage> CreateMessages(int count)
    {
        var result = new List<QuorumQueueMessage>();
        for (var i = 0; i < count; i++)
            result.Add(new QuorumQueueMessage(i));
        return result;
    }
}

[Queue(Name = "QuorumQueue", Type = QueueType.Quorum)]
public class QuorumQueueMessage : Message
{
    public QuorumQueueMessage(int id) : base(id)
    {
    }
}
