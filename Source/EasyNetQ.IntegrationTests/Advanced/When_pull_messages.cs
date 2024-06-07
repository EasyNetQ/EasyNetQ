using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_pull_messages : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_pull_messages(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public void Dispose()
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

        using var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeTrue();
            await consumer.AckAsync(
                pullResult.ReceivedInfo.DeliveryTag, cts.Token
            );
        }

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeFalse();
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
            Exchange.Default, queue.Name, false, false, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeTrue();
            await consumer.RejectAsync(
                pullResult.ReceivedInfo.DeliveryTag, false, cts.Token
            );
        }

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeFalse();
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
            Exchange.Default, queue.Name, false, false, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeTrue();
            await consumer.RejectAsync(
                pullResult.ReceivedInfo.DeliveryTag, true, cts.Token
            );
        }

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeTrue();
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
            Exchange.Default, queue.Name, false, false, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );

        using var consumer = bus.Advanced.CreatePullingConsumer(queue);

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeTrue();
        }

        {
            using var pullResult = await consumer.PullAsync(cts.Token);
            pullResult.IsAvailable.Should().BeFalse();
        }
    }
}
