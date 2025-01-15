using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_pull_messages_batch : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_pull_messages_batch(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Should_be_able_ack()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = await bus.Advanced.QueueDeclareAsync(
            queue: Guid.NewGuid().ToString("N"), cancellationToken: cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(2);
            await consumer.AckBatchAsync(
                pullResult.DeliveryTag, cts.Token
            );
        }

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(0);
        }
    }

    [Fact]
    public async Task Should_be_able_reject()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = await bus.Advanced.QueueDeclareAsync(
            queue: Guid.NewGuid().ToString("N"), cancellationToken: cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(2);
            await consumer.RejectBatchAsync(
                pullResult.DeliveryTag, false, cts.Token
            );
        }

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(0);
        }
    }

    [Fact]
    public async Task Should_be_able_reject_with_requeue()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = await bus.Advanced.QueueDeclareAsync(
            queue: Guid.NewGuid().ToString("N"), cancellationToken: cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(2);
            await consumer.RejectBatchAsync(
                pullResult.DeliveryTag, true, cts.Token
            );
        }

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Should_be_able_with_auto_ack()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = await bus.Advanced.QueueDeclareAsync(
            queue: Guid.NewGuid().ToString("N"), cancellationToken: cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );
        await bus.Advanced.PublishAsync(
            Exchange.Default, queue.Name, false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue);

        {
            using var pullResult = await consumer.PullBatchAsync(2, cts.Token);
            pullResult.Messages.Should().HaveCount(2);
        }

        {
            using var pullResult = await consumer.PullBatchAsync(0, cts.Token);
            pullResult.Messages.Should().HaveCount(0);
        }
    }
}
