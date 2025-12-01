using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public abstract class ConsumerTestBase : IAsyncLifetime
{
    protected const string ConsumerTag = "the_consumer_tag";
    protected const ulong DeliverTag = 10101;
    protected readonly IConsumeErrorStrategy ConsumeErrorStrategy;
    protected readonly MockBuilder MockBuilder;
    protected bool ConsumerWasInvoked;
    protected ReadOnlyMemory<byte> DeliveredMessageBody;
    protected MessageReceivedInfo DeliveredMessageInfo;
    protected MessageProperties DeliveredMessageProperties;
    protected byte[] OriginalBody;

    // populated when a message is delivered
    protected IBasicProperties OriginalProperties;

    protected ConsumerTestBase()
    {
        ConsumeErrorStrategy = Substitute.For<IConsumeErrorStrategy>();
        MockBuilder = new MockBuilder(x => x.AddSingleton(ConsumeErrorStrategy));
    }

    public Task InitializeAsync() => InitializeAsyncCore();

    protected virtual Task InitializeAsyncCore() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await MockBuilder.DisposeAsync();
    }

    protected async Task<IAsyncDisposable> StartConsumerAsync(
        Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, AckStrategyAsync> handler,
        bool autoAck = false
    )
    {
        ConsumerWasInvoked = false;
        var queue = new Queue("my_queue", false);
        return await MockBuilder.Bus.Advanced.ConsumeAsync(
            queue,
            (body, properties, messageInfo, ct) =>
            {
                return Task.Run(() =>
                {
                    DeliveredMessageBody = body;
                    DeliveredMessageProperties = properties;
                    DeliveredMessageInfo = messageInfo;

                    var ackStrategy = handler(body, properties, messageInfo, ct);
                    ConsumerWasInvoked = true;
                    return ackStrategy;
                }, CancellationToken.None);
            },
            c =>
            {
                if (autoAck)
                    c.WithAutoAck();
                c.WithConsumerTag(ConsumerTag);
            }
        );
    }

    protected Task DeliverMessageAsync()
    {
        OriginalProperties = new RabbitMQ.Client.BasicProperties
        {
            Type = "the_message_type",
            CorrelationId = "the_correlation_id",
        };
        OriginalBody = "Hello World"u8.ToArray();

        return MockBuilder.Consumers[0].HandleBasicDeliverAsync(
            ConsumerTag,
            DeliverTag,
            false,
            "the_exchange",
            "the_routing_key",
            OriginalProperties,
            OriginalBody
        );
    }
}
