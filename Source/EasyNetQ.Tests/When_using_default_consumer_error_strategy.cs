using System.Text;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

public class When_using_default_consumer_error_strategy
{
    public When_using_default_consumer_error_strategy()
    {
        customConventions = new Conventions(new DefaultTypeNameSerializer())
        {
            ErrorQueueNamingConvention = _ => "CustomEasyNetQErrorQueueName",
            ErrorExchangeNamingConvention = info => "CustomErrorExchangePrefixName." + info.RoutingKey,
            ErrorQueueTypeConvention = () => QueueType.Quorum,
            ErrorExchangeTypeConvention = () => ExchangeType.Topic,
            ErrorExchangeRoutingKeyConvention = _ => "CustomRoutingKey"
        };

        const string originalMessage = "";
        var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

        consumerExecutionContext = new ConsumerExecutionContext(
            (_, _, _, _) => Task.FromResult(AckStrategies.Ack),
            new MessageReceivedInfo("consumerTag", 0, false, "originalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = string.Empty,
                AppId = string.Empty
            },
            originalMessageBody
        );
    }

    private readonly ConsumerExecutionContext consumerExecutionContext;
    private readonly Conventions customConventions;

    [Fact]
    public async Task Should_Ack_canceled_message()
    {
        using var mockBuilder = new MockBuilder(x => x.Register<IConventions>(customConventions));

        var cancelAckStrategy = await mockBuilder.ConsumerErrorStrategy.HandleConsumerCancelledAsync(consumerExecutionContext, default);

        Assert.Same(AckStrategies.NackWithRequeue, cancelAckStrategy);
    }

    [Fact]
    public async Task Should_Ack_failed_message_When_publish_confirms_off()
    {
        using var mockBuilder = new MockBuilder(x => x.Register<IConventions>(customConventions));

        var errorAckStrategy = await mockBuilder.ConsumerErrorStrategy.HandleConsumerErrorAsync(consumerExecutionContext, new Exception());

        Assert.Same(AckStrategies.Ack, errorAckStrategy);

        mockBuilder.Channels[0].Received().ExchangeDeclare("CustomErrorExchangePrefixName.originalRoutingKey", "topic", true);
        mockBuilder.Channels[0].Received().QueueDeclare(
            "CustomEasyNetQErrorQueueName",
            true,
            false,
            false,
            Arg.Is<IDictionary<string, object>>(x => x.ContainsKey("x-queue-type") && x["x-queue-type"].Equals(QueueType.Quorum))
        );
        mockBuilder.Channels[0].Received().QueueBind(
            "CustomEasyNetQErrorQueueName",
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "CustomRoutingKey",
            null
        );
        mockBuilder.Channels[0].Received().BasicPublish(
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "originalRoutingKey",
            false,
            Arg.Any<IBasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>()
        );
    }

    [Fact]
    public async Task Should_Ack_failed_message_When_publish_confirms_on()
    {
        using var mockBuilder = new MockBuilder(
            x => x.Register<IConventions>(customConventions)
                .Register(_ => new ConnectionConfiguration { PublisherConfirms = true })
        );

        var errorAckStrategy = await mockBuilder.ConsumerErrorStrategy.HandleConsumerErrorAsync(consumerExecutionContext, new Exception());

        Assert.Same(AckStrategies.Ack, errorAckStrategy);

        mockBuilder.Channels[0].Received().ConfirmSelect();
        mockBuilder.Channels[0].Received().ExchangeDeclare("CustomErrorExchangePrefixName.originalRoutingKey", "topic", true);
        mockBuilder.Channels[0].Received().QueueDeclare(
            "CustomEasyNetQErrorQueueName",
            true,
            false,
            false,
            Arg.Is<IDictionary<string, object>>(x => x.ContainsKey("x-queue-type") && x["x-queue-type"].Equals(QueueType.Quorum))
        );
        mockBuilder.Channels[0].Received().QueueBind(
            "CustomEasyNetQErrorQueueName",
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "CustomRoutingKey",
            null
        );
        mockBuilder.Channels[0].Received().BasicPublish(
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "originalRoutingKey",
            false,
            Arg.Any<IBasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>()
        );
        mockBuilder.Channels[0].Received().WaitForConfirms(TimeSpan.FromSeconds(10));
    }
}
