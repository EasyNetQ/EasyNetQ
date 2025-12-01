using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_publish_confirms_and_multi_channel_dispatcher : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_publish_confirms_and_multi_channel_dispatcher(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;publisherConfirms=True")
            .UseMultiChannelClientCommandDispatcher(2);

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    private const int MessagesCount = 20;

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
