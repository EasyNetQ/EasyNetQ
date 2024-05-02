using EasyNetQ.Consumer;
using EasyNetQ.DI;
using MS = Microsoft.Extensions.Logging;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.InternalConsumerTests;

public class InternalConsumerTests : IDisposable
{
    private readonly Queue exclusiveQueue = new("exclusive", isExclusive: true);
    private readonly Queue nonExclusiveQueue = new("non-exclusive", isExclusive: false);

    private readonly MockBuilder mockBuilder;
    private readonly InternalConsumer internalConsumer;

    public InternalConsumerTests()
    {
        mockBuilder = new MockBuilder();

        internalConsumer = new InternalConsumer(
            Substitute.For<IServiceResolver>(),
            Substitute.For<MS.ILogger<InternalConsumer>>(),
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
                            _ => new ValueTask<AckStrategy>(AckStrategies.Ack)
                        )
                    },
                    {
                        nonExclusiveQueue,
                        new PerQueueConsumerConfiguration(
                            false,
                            "nonExclusiveConsumerTag",
                            false,
                            new Dictionary<string, object>(),
                            _ => new ValueTask<AckStrategy>(AckStrategies.Ack)
                        )
                    }
                }
            ),
            mockBuilder.ConsumerConnection,
            Substitute.For<IEventBus>()
        );
    }

    [Fact]
    public void Should_follow_reconnection_lifecycle()
    {
        var status = internalConsumer.StartConsuming(true);
        status.Started.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Active.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Failed.Should().BeEmpty();

        internalConsumer.StopConsuming();

        status = internalConsumer.StartConsuming(false);

        status.Started.Should().BeEquivalentTo(new[] { nonExclusiveQueue });
        status.Active.Should().BeEquivalentTo(new[] { nonExclusiveQueue });
        status.Failed.Should().BeEquivalentTo(new[] { exclusiveQueue });

        internalConsumer.StopConsuming();
    }

    [Fact]
    public void Should_follow_lifecycle_without_reconnections()
    {
        var status = internalConsumer.StartConsuming(true);

        status.Started.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Active.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Failed.Should().BeEmpty();

        status = internalConsumer.StartConsuming(false);
        status.Started.Should().BeEmpty();
        status.Active.Should().BeEquivalentTo(new[] { exclusiveQueue, nonExclusiveQueue });
        status.Failed.Should().BeEmpty();

        internalConsumer.StopConsuming();
    }

    public void Dispose()
    {
        mockBuilder?.Dispose();
        internalConsumer?.Dispose();
    }
}
