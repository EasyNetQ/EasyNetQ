using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_priority : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_priority(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1");

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

    private const byte LowPriority = 1;
    private const byte HighPriority = 2;
    private const int MessagesCount = 10;

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var messagesSink = new MessagesSink(MessagesCount * 2);
        var highPriorityMessages = MessagesFactories.Create(MessagesCount);
        var lowPriorityMessages = MessagesFactories.Create(MessagesCount, MessagesCount);

        var subscriptionId = Guid.NewGuid().ToString();
        await using (
            await bus.PubSub.SubscribeAsync<Message>(
                subscriptionId, messagesSink.Receive, x => x.WithMaxPriority(2), cts.Token
            )
        )
        {
        }

        await bus.PubSub.PublishBatchAsync(
            lowPriorityMessages, x => x.WithPriority(LowPriority), cts.Token
        );
        await bus.PubSub.PublishBatchAsync(
            highPriorityMessages, x => x.WithPriority(HighPriority), cts.Token
        );

        await using (
            await bus.PubSub.SubscribeAsync<Message>(
                subscriptionId, messagesSink.Receive, x => x.WithMaxPriority(2), cts.Token
            )
        )
        {
            await messagesSink.WaitAllReceivedAsync(cts.Token);

            messagesSink.ReceivedMessages.Should().Equal(highPriorityMessages.Concat(lowPriorityMessages));
        }
    }
}
