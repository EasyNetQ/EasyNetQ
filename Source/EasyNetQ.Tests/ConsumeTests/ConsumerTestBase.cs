using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public abstract class ConsumerTestBase : IDisposable
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

    public ConsumerTestBase()
    {
        ConsumeErrorStrategy = Substitute.For<IConsumeErrorStrategy>();
        MockBuilder = new MockBuilder(x => x.Register(ConsumeErrorStrategy));
        AdditionalSetUp();
    }

    public void Dispose()
    {
        MockBuilder.Dispose();
    }

    protected abstract void AdditionalSetUp();

    protected IDisposable StartConsumer(
        Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, AckStrategy> handler,
        bool autoAck = false
    )
    {
        ConsumerWasInvoked = false;
        var queue = new Queue("my_queue", false);
        return MockBuilder.Bus.Advanced.Consume(
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

    protected void DeliverMessage()
    {
        DeliverMessageAsync().GetAwaiter().GetResult();
    }

    protected Task DeliverMessageAsync()
    {
        OriginalProperties = new BasicProperties
        {
            Type = "the_message_type",
            CorrelationId = "the_correlation_id",
        };
        OriginalBody = "Hello World"u8.ToArray();

        return MockBuilder.Consumers[0].HandleBasicDeliver(
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
