using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.InternalConsumerTests;

public sealed class When_internal_consumer_consumes_queues_with_different_prefetch : IAsyncLifetime
{
    private readonly Queue lowPrefetchQueue = new("low", isExclusive: false);
    private readonly Queue highPrefetchQueue = new("high", isExclusive: false);
    private readonly Queue defaultPrefetchQueue = new("default", isExclusive: false);
    private readonly MockBuilder mockBuilder = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_set_each_queue_prefetch_before_its_basic_consume()
    {
        await using var internalConsumer = CreateInternalConsumer();

        var status = await internalConsumer.StartConsumingAsync(true, CancellationToken.None);

        status.Failed.Should().BeEmpty();
        var channel = mockBuilder.Channels[0];
        Received.InOrder(() =>
        {
            channel.BasicQosAsync(0, 1, false, Arg.Any<CancellationToken>());
            channel.BasicConsumeAsync("low", Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<IAsyncBasicConsumer>(), Arg.Any<CancellationToken>());
            channel.BasicQosAsync(0, 50, false, Arg.Any<CancellationToken>());
            channel.BasicConsumeAsync("high", Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<IAsyncBasicConsumer>(), Arg.Any<CancellationToken>());
            channel.BasicQosAsync(0, 10, false, Arg.Any<CancellationToken>());
            channel.BasicConsumeAsync("default", Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<IAsyncBasicConsumer>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Should_not_repeat_qos_when_the_prefetch_is_unchanged()
    {
        await using var internalConsumer = CreateInternalConsumer(highPrefetch: 1);

        await internalConsumer.StartConsumingAsync(true, CancellationToken.None);

        await mockBuilder.Channels[0].Received(1).BasicQosAsync(0, 1, false, Arg.Any<CancellationToken>());
    }

    private InternalConsumer CreateInternalConsumer(ushort highPrefetch = 50) => new(
        Substitute.For<IServiceProvider>(),
        Substitute.For<ILogger<InternalConsumer>>(),
        new ConsumerConfiguration(
            10,
            new Dictionary<Queue, PerQueueConsumerConfiguration>
            {
                { lowPrefetchQueue, PerQueue(lowPrefetchQueue, 1) },
                { highPrefetchQueue, PerQueue(highPrefetchQueue, highPrefetch) },
                { defaultPrefetchQueue, PerQueue(defaultPrefetchQueue, null) },
            }
        ),
        mockBuilder.ConsumerConnection,
        Substitute.For<IEventBus>()
    );

    private static PerQueueConsumerConfiguration PerQueue(Queue queue, ushort? prefetchCount) => new(
        false, "", false, new Dictionary<string, object>(), TestContexts.Consumer(queue.Name), prefetchCount
    );
}
