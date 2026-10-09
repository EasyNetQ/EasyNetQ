using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests.InternalConsumerTests;

public sealed class When_internal_consumer_creates_its_channel : IAsyncLifetime
{
    private readonly Queue queue = new("my_queue", isExclusive: false);
    private readonly MockBuilder mockBuilder = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_set_consumer_dispatch_concurrency_on_the_channel_when_configured()
    {
        await using var internalConsumer = CreateInternalConsumer(consumerDispatcherConcurrency: 3);

        await internalConsumer.StartConsumingAsync(true, CancellationToken.None);

        await mockBuilder.Connection.Received(1).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x != null && x.ConsumerDispatchConcurrency == 3),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_not_enable_publisher_confirms_on_the_channel_when_concurrency_is_configured()
    {
        await using var internalConsumer = CreateInternalConsumer(consumerDispatcherConcurrency: 3);

        await internalConsumer.StartConsumingAsync(true, CancellationToken.None);

        await mockBuilder.Connection.Received(1).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x != null && !x.PublisherConfirmationsEnabled && !x.PublisherConfirmationTrackingEnabled),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_create_the_channel_without_options_when_concurrency_is_not_configured()
    {
        // Without options the channel inherits the connection-wide ConsumerDispatchConcurrency, as before.
        // Passing CreateChannelOptions instead would not be equivalent: its constructor defaults the concurrency to 1.
        await using var internalConsumer = CreateInternalConsumer(consumerDispatcherConcurrency: null);

        await internalConsumer.StartConsumingAsync(true, CancellationToken.None);

        await mockBuilder.Connection.Received(1).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_keep_consumer_dispatch_concurrency_when_the_channel_is_recreated_after_a_soft_error()
    {
        await using var internalConsumer = CreateInternalConsumer(consumerDispatcherConcurrency: 3);
        await internalConsumer.StartConsumingAsync(true, CancellationToken.None);
        mockBuilder.Channels[0].CloseReason.Returns(
            new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.PreconditionFailed, "Oops")
        );

        await internalConsumer.StartConsumingAsync(false, CancellationToken.None);

        mockBuilder.Channels.Should().HaveCount(2);
        await mockBuilder.Connection.Received(2).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x != null && x.ConsumerDispatchConcurrency == 3),
            Arg.Any<CancellationToken>()
        );
    }

    private InternalConsumer CreateInternalConsumer(ushort? consumerDispatcherConcurrency) => new(
        Substitute.For<IServiceProvider>(),
        Substitute.For<ILogger<InternalConsumer>>(),
        new ConsumerConfiguration(
            42,
            new Dictionary<Queue, PerQueueConsumerConfiguration>
            {
                {
                    queue,
                    new PerQueueConsumerConfiguration(
                        false,
                        "consumerTag",
                        false,
                        new Dictionary<string, object>(),
                        null
                    )
                }
            },
            consumerDispatcherConcurrency
        ),
        mockBuilder.ConsumerConnection,
        Substitute.For<IEventBus>()
    );
}
