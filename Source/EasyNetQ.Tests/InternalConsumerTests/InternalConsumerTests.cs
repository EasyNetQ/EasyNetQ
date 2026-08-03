using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Tests.InternalConsumerTests;

public sealed class InternalConsumerTests : IAsyncLifetime
{
    private readonly Queue exclusiveQueue = new("exclusive", isExclusive: true);
    private readonly Queue nonExclusiveQueue = new("non-exclusive", isExclusive: false);

    private readonly MockBuilder mockBuilder;
    private readonly InternalConsumer internalConsumer;

    public InternalConsumerTests()
    {
        mockBuilder = new MockBuilder();

        internalConsumer = new InternalConsumer(
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<InternalConsumer>>(),
            new ConsumerConfiguration(
                42,
                new Dictionary<Queue, PerQueueConsumerConfiguration>
                {
                    {
                        exclusiveQueue,
                        new PerQueueConsumerConfiguration(
                            false,
                            "exclusiveConsumerTag",
                            false,
                            new Dictionary<string, object>(),
                            _ => new ValueTask<AckStrategyAsync>(AckStrategies.AckAsync)
                        )
                    },
                    {
                        nonExclusiveQueue,
                        new PerQueueConsumerConfiguration(
                            false,
                            "nonExclusiveConsumerTag",
                            false,
                            new Dictionary<string, object>(),
                            _ => new ValueTask<AckStrategyAsync>(AckStrategies.AckAsync)
                        )
                    }
                }
            ),
            mockBuilder.ConsumerConnection,
            Substitute.For<IEventBus>()
        );
    }

    [Fact]
    public async Task Should_follow_reconnection_lifecycle_async()
    {
        var status = await internalConsumer.StartConsumingAsync(true, cancellationToken: TestContext.Current.CancellationToken);
        status.Started.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Active.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Failed.Should().BeEmpty();

        await internalConsumer.StopConsumingAsync(cancellationToken: TestContext.Current.CancellationToken);

        status = await internalConsumer.StartConsumingAsync(false, cancellationToken: TestContext.Current.CancellationToken);
        status.Started.Should().BeEquivalentTo(new[] { nonExclusiveQueue });
        status.Active.Should().BeEquivalentTo(new[] { nonExclusiveQueue });
        status.Failed.Should().BeEquivalentTo(new[] { exclusiveQueue });

        await internalConsumer.StopConsumingAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_follow_lifecycle_without_reconnections_async()
    {
        var status = await internalConsumer.StartConsumingAsync(true, cancellationToken: TestContext.Current.CancellationToken);
        status.Started.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Active.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Failed.Should().BeEmpty();

        status = await internalConsumer.StartConsumingAsync(false, cancellationToken: TestContext.Current.CancellationToken);
        status.Started.Should().BeEmpty();
        status.Active.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Failed.Should().BeEmpty();

        await internalConsumer.StopConsumingAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
        await internalConsumer.DisposeAsync();
    }
}
