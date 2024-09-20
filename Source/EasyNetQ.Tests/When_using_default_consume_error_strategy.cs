using System.Text;
using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

public class When_using_default_consume_error_strategy
{
    public When_using_default_consume_error_strategy()
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

        consumerExecutionContext = new ConsumeContext(
            new MessageReceivedInfo("consumerTag", 0, false, "originalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = string.Empty,
                AppId = string.Empty
            },
            originalMessageBody,
            Substitute.For<IServiceProvider>(),
            CancellationToken.None
        );
    }

    private readonly ConsumeContext consumerExecutionContext;
    private readonly Conventions customConventions;

    [Fact]
    public async Task Should_Ack_canceled_message()
    {
        using var mockBuilder = new MockBuilder(x => x.AddSingleton<IConventions>(customConventions));

        var cancelAckStrategy = await mockBuilder.ConsumeErrorStrategy.HandleCancelledAsync(consumerExecutionContext);

        Assert.Same(AckStrategies.NackWithRequeueAsync, cancelAckStrategy);
    }

    [Fact]
    public async Task Should_Ack_failed_message_When_publish_confirms_off()
    {
        using var mockBuilder = new MockBuilder(x => x.AddSingleton<IConventions>(customConventions));

        var errorAckStrategy = await mockBuilder.ConsumeErrorStrategy.HandleErrorAsync(consumerExecutionContext, new Exception());

        Assert.Same(AckStrategies.AckAsync, errorAckStrategy);

        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync("CustomErrorExchangePrefixName.originalRoutingKey", "topic", true);
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            "CustomEasyNetQErrorQueueName",
            true,
            false,
            false,
            Arg.Is<IDictionary<string, object>>(x => x.ContainsKey("x-queue-type") && x["x-queue-type"].Equals(QueueType.Quorum))
        );
        await mockBuilder.Channels[0].Received().QueueBindAsync(
            "CustomEasyNetQErrorQueueName",
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "CustomRoutingKey",
            null
        );

        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "originalRoutingKey",
            false,
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_Ack_failed_message_When_publish_confirms_on()
    {
        using var mockBuilder = new MockBuilder(
            x => x.AddSingleton<IConventions>(customConventions)
                .AddSingleton(_ => new ConnectionConfiguration { PublisherConfirms = true })
        );

        var errorAckStrategy = await mockBuilder.ConsumeErrorStrategy.HandleErrorAsync(consumerExecutionContext, new Exception());

        Assert.Same(AckStrategies.AckAsync, errorAckStrategy);

        await mockBuilder.Channels[0].Received().ConfirmSelectAsync();
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync("CustomErrorExchangePrefixName.originalRoutingKey", "topic", true);
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            "CustomEasyNetQErrorQueueName",
            true,
            false,
            false,
            Arg.Is<IDictionary<string, object>>(x => x.ContainsKey("x-queue-type") && x["x-queue-type"].Equals(QueueType.Quorum)),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
        await mockBuilder.Channels[0].Received().QueueBindAsync(
            "CustomEasyNetQErrorQueueName",
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "CustomRoutingKey",
            null
        );

        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            "CustomErrorExchangePrefixName.originalRoutingKey",
            "originalRoutingKey",
            false,
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>()
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await mockBuilder.Channels[0].Received().WaitForConfirmsAsync(cts.Token);
    }
}
