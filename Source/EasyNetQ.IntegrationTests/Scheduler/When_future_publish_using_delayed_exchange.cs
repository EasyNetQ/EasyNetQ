using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Scheduler;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_delay_using_delay_exchange : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_delay_using_delay_exchange(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1")
            .UseDelayedExchangeScheduler();

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);

        await using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, cts.Token))
        {
            await bus.Scheduler.FuturePublishBatchAsync(messages, TimeSpan.FromSeconds(5), "#", cts.Token)
                ;

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().Equal(messages);
        }
    }
}
