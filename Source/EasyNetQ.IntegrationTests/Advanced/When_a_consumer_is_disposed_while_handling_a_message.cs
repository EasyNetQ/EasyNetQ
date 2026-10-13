using System.Collections.Concurrent;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_a_consumer_is_disposed_while_handling_a_message : IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_a_consumer_is_disposed_while_handling_a_message(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    [Fact]
    public async Task Should_ack_the_message_instead_of_redelivering_it()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cancellationToken: cts.Token);

        var firstMessageReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lastMessageReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedMessages = new ConcurrentQueue<int>();

        var handlingConsumer = await bus.Advanced.ConsumeAsync(queue, async (_, _, _, _) =>
        {
            firstMessageReceived.TrySetResult();
            // Ignores the cancellation token, so the message is still being handled when the consumer is disposed
            await Task.Delay(TimeSpan.FromSeconds(1));
        });

        await bus.Advanced.PublishAsync(
            Exchange.Default, queueName, true, true, MessageProperties.Empty, new[] { (byte)0 }, cts.Token
        );
        await firstMessageReceived.Task.WaitAsync(cts.Token);

        await using (
            await bus.Advanced.ConsumeAsync(queue, (body, _, _, _) =>
            {
                receivedMessages.Enqueue(body.Span[0]);
                if (body.Span[0] == 1)
                    lastMessageReceived.TrySetResult();
                return Task.CompletedTask;
            })
        )
        {
            await handlingConsumer.DisposeAsync();
            await bus.Advanced.PublishAsync(
                Exchange.Default, queueName, true, true, MessageProperties.Empty, new[] { (byte)1 }, cts.Token
            );
            await lastMessageReceived.Task.WaitAsync(cts.Token);
        }

        receivedMessages.Should().Equal(1);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }
}
