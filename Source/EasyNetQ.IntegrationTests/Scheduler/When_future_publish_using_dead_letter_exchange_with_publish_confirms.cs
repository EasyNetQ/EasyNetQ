using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Scheduler;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_using_delay_using_dead_letter_exchange_with_publish_confirms : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_using_delay_using_dead_letter_exchange_with_publish_confirms(
        RabbitMQFixture fixture
    )
    {
        var connectionString = $"host={fixture.Host};prefetchCount=1;publisherConfirms=True;timeout=-1";
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ(connectionString)
            .UseDelayedExchangeScheduler();

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
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        var subscriptionId = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);

        await using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, cts.Token))
        {
            await bus.Scheduler.FuturePublishBatchAsync(messages, TimeSpan.FromSeconds(5), "#", cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().Equal(messages);
        }
    }
}
