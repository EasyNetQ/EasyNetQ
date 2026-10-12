using System.Collections.Concurrent;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_the_active_single_active_consumer_is_disposed : IAsyncLifetime
{
    private const int MessagesCount = 3;

    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_the_active_single_active_consumer_is_disposed(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    [Fact]
    public async Task Should_redeliver_its_unacked_message_before_the_next_ones()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(
            queueName, arguments: new Dictionary<string, object>().WithSingleActiveConsumer(), cancellationToken: cts.Token
        );

        var firstMessageReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedMessages = new ConcurrentQueue<int>();
        using var allMessagesReceived = new AsyncCountdownEvent();

        var activeConsumer = await bus.Advanced.ConsumeAsync(queue, (_, _, _, ct) =>
        {
            firstMessageReceived.TrySetResult();
            // Holds the first message unacked until the consumer is disposed
            return Task.Delay(-1, ct);
        });

        await using (
            await bus.Advanced.ConsumeAsync(queue, (body, _, _, _) =>
            {
                receivedMessages.Enqueue(body.Span[0]);
                allMessagesReceived.Decrement();
                return Task.CompletedTask;
            })
        )
        {
            for (var i = 0; i < MessagesCount; i++)
            {
                await bus.Advanced.PublishAsync(
                    Exchange.Default, queueName, true, true, MessageProperties.Empty, new[] { (byte)i }, cts.Token
                );
                allMessagesReceived.Increment();
            }

            await firstMessageReceived.Task.WaitAsync(cts.Token);
            await activeConsumer.DisposeAsync();
            await allMessagesReceived.WaitAsync(cts.Token);
        }

        receivedMessages.Should().Equal(0, 1, 2);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }
}
