using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_exclusive : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_exclusive(RabbitMQFixture fixture)
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

        var firstConsumerMessagesSink = new MessagesSink(MessagesCount);
        var secondConsumerMessagesSink = new MessagesSink(0);

        var messages = MessagesFactories.Create(MessagesCount);

        using (
            await bus.PubSub.SubscribeAsync<Message>(
                Guid.NewGuid().ToString(),
                firstConsumerMessagesSink.Receive,
                x => x.AsExclusive(),
                cts.Token
            )
        )
        {
            // To ensure that ^ subscriber started successfully
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    Guid.NewGuid().ToString(),
                    secondConsumerMessagesSink.Receive,
                    x => x.AsExclusive(),
                    cts.Token
                )
            )
            {
                await bus.PubSub.PublishBatchAsync(messages, cts.Token);

                await Task.WhenAll(
                    firstConsumerMessagesSink.WaitAllReceivedAsync(cts.Token),
                    secondConsumerMessagesSink.WaitAllReceivedAsync(cts.Token)
                );

                firstConsumerMessagesSink.ReceivedMessages.Should().Equal(messages);
                secondConsumerMessagesSink.ReceivedMessages.Should().Equal();
            }
        }
    }
}
