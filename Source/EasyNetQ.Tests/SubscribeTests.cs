using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

#pragma warning disable IDISP006
public class When_subscribe_is_called : IAsyncLifetime
{
    private const string typeName = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests";
    private const string subscriptionId = "the_subscription_id";
    private const string queueName = typeName + "_" + subscriptionId;
    private const string consumerTag = "the_consumer_tag";

    private readonly MockBuilder mockBuilder;
    private SubscriptionResult subscriptionResult;

    public When_subscribe_is_called()
    {
        var conventions = new Conventions(new DefaultTypeNameSerializer())
        {
            ConsumerTagConvention = () => consumerTag
        };

        mockBuilder = new MockBuilder(x => x
            .AddSingleton<IConventions>(conventions)
        );
    }

    public async Task InitializeAsync()
    {
        subscriptionResult = await mockBuilder.PubSub.SubscribeAsync<MyMessage>(subscriptionId, _ => { });
    }

    public async Task DisposeAsync()
    {
        await subscriptionResult.DisposeAsync();
        
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_create_a_new_channel_for_the_consumer()
    {
        // A channel is created for running client originated commands,
        // a second channel is created for the consumer.
        mockBuilder.Channels.Count.Should().Be(3);
    }

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[1].Received().QueueDeclareAsync(
            Arg.Is(queueName),
            Arg.Is(true), // durable
            Arg.Is(false), // exclusive
            Arg.Is(false), // autoDelete
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_declare_the_exchange()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is(typeName),
            Arg.Is("topic"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Is((IDictionary<string, object>)null),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_bind_the_queue_and_exchange()
    {
        await mockBuilder.Channels[1].Received().QueueBindAsync(
            Arg.Is(queueName),
            Arg.Is(typeName),
            Arg.Is("#"),
            Arg.Is((IDictionary<string, object>)null),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_set_configured_prefetch_count()
    {
        var connectionConfiguration = new ConnectionConfiguration();
        await mockBuilder.Channels[2].Received().BasicQosAsync(0, connectionConfiguration.PrefetchCount, false);
    }

    [Fact]
    public async Task Should_start_consuming()
    {
        await mockBuilder.Channels[2].Received().BasicConsumeAsync(
            Arg.Is(queueName),
            Arg.Is(false),
            Arg.Any<string>(),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_register_consumer()
    {
        mockBuilder.Consumers.Count.Should().Be(1);
    }

    [Fact]
    public void Should_return_non_null_and_with_expected_values_result()
    {
        Assert.True(subscriptionResult.Exchange.Name == typeName);
        Assert.True(subscriptionResult.Queue.Name == queueName);
    }
}

public class When_subscribe_with_configuration_is_called
{
    [InlineData("ttt", true, 99, 999, 10, true, (byte)11, false, "qqq", 1001, 10001)]
    [InlineData(null, false, 0, 0, null, false, null, true, "qqq", null, null)]
    [Theory]
    public async Task Queue_should_be_declared_with_correct_options(
        string topic,
        bool autoDelete,
        int priority,
        ushort prefetchCount,
        int? expires,
        bool isExclusive,
        byte? maxPriority,
        bool durable,
        string queueName,
        int? maxLength,
        int? maxLengthBytes
    )
    {
        await using var mockBuilder = new MockBuilder();
        // Configure subscription
        await mockBuilder.PubSub.SubscribeAsync<MyMessage>(
            "x",
            _ => { },
            c =>
            {
                c.WithAutoDelete(autoDelete)
                    .WithPriority(priority)
                    .WithPrefetchCount(prefetchCount)
                    .AsExclusive(isExclusive)
                    .WithDurable(durable)
                    .WithQueueName(queueName);

                if (topic != null)
                {
                    c.WithTopic(topic);
                }

                if (maxPriority.HasValue)
                {
                    c.WithMaxPriority(maxPriority.Value);
                }

                if (expires.HasValue)
                {
                    c.WithExpires(expires.Value);
                }

                if (maxLength.HasValue)
                {
                    c.WithMaxLength(maxLength.Value);
                }

                if (maxLengthBytes.HasValue)
                {
                    c.WithMaxLengthBytes(maxLengthBytes.Value);
                }
            }
        );

        // Assert that queue got declared correctly
        await mockBuilder.Channels[1].Received().QueueDeclareAsync(
            Arg.Is(queueName ?? "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests_x"),
            Arg.Is(durable),
            Arg.Is(false),
            Arg.Is(autoDelete),
            Arg.Is<IDictionary<string, object>>(
                x => (!expires.HasValue || expires.Value == (int)x["x-expires"]) &&
                     (!maxPriority.HasValue || maxPriority.Value == (byte)x["x-max-priority"]) &&
                     (!maxLength.HasValue || maxLength.Value == (int)x["x-max-length"]) &&
                     (!maxLengthBytes.HasValue || maxLengthBytes.Value == (int)x["x-max-length-bytes"])
            ),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );

        // Assert that consumer was created correctly
        await mockBuilder.Channels[2].Received().BasicConsumeAsync(
            Arg.Is(queueName ?? "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests_x"),
            Arg.Is(false),
            Arg.Any<string>(),
            Arg.Is(true),
            Arg.Is(isExclusive),
            Arg.Is<IDictionary<string, object>>(x => priority == (int)x["x-priority"]),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>()
        );

        // Assert that QoS got configured correctly
        await mockBuilder.Channels[2].Received().BasicQosAsync(0, prefetchCount, false);

        // Assert that binding got configured correctly
        await mockBuilder.Channels[1].Received().QueueBindAsync(
            Arg.Is(queueName),
            Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
            Arg.Is(topic ?? "#"),
            Arg.Is((IDictionary<string, object>)null),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }
}

public class When_a_message_is_delivered : IAsyncLifetime
{
    private const string typeName = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests";
    private const string subscriptionId = "the_subscription_id";
    private const string correlationId = "the_correlation_id";
    private const string consumerTag = "the_consumer_tag";
    private const ulong deliveryTag = 123;
    private MyMessage deliveredMessage;
    private readonly MockBuilder mockBuilder;
    private MyMessage originalMessage;

    public When_a_message_is_delivered()
    {
        var conventions = new Conventions(new DefaultTypeNameSerializer())
        {
            ConsumerTagConvention = () => consumerTag
        };

        mockBuilder = new MockBuilder(x => x.AddSingleton<IConventions>(conventions));


    }

    public async Task InitializeAsync()
    {
#pragma warning disable IDISP004
        await mockBuilder.PubSub.SubscribeAsync<MyMessage>(subscriptionId, message => { deliveredMessage = message; });
#pragma warning restore IDISP004

        const string text = "Hello there, I am the text!";
        originalMessage = new MyMessage { Text = text };

        using var serializedMessage = new ReflectionBasedNewtonsoftJsonSerializer().MessageToBytes(typeof(MyMessage), originalMessage);

        // deliver a message
        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            consumerTag,
            deliveryTag,
            false, // redelivered
            typeName,
            "#",
            new BasicProperties
            {
                Type = typeName,
                CorrelationId = correlationId
            },
            serializedMessage.Memory
        );
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_build_bus_successfully()
    {
        // just want to run SetUp()
    }

    [Fact]
    public void Should_deliver_message()
    {
        deliveredMessage.Should().NotBeNull();
        deliveredMessage.Text.Should().Be(originalMessage.Text);
    }

    [Fact]
    public async Task Should_ack_the_message()
    {
        await mockBuilder.Channels[2].Received().BasicAckAsync(deliveryTag, false);
    }
}

public class When_the_handler_throws_an_exception : IAsyncLifetime
{
    private const string typeName = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests";
    private const string subscriptionId = "the_subscription_id";
    private const string correlationId = "the_correlation_id";
    private const string consumerTag = "the_consumer_tag";
    private const ulong deliveryTag = 123;
    private readonly Exception originalException = new("Some exception message");

    private MyMessage originalMessage;
    private ConsumeContext basicDeliverEventArgs;
    private readonly IConsumeErrorStrategy consumeErrorStrategy;
    private readonly MockBuilder mockBuilder;
    private Exception raisedException;

    public When_the_handler_throws_an_exception()
    {
        var conventions = new Conventions(new DefaultTypeNameSerializer())
        {
            ConsumerTagConvention = () => consumerTag
        };

        consumeErrorStrategy = Substitute.For<IConsumeErrorStrategy>();
        consumeErrorStrategy.HandleErrorAsync(Arg.Any<ConsumeContext>(), Arg.Any<Exception>())
            .ReturnsForAnyArgs(
                i =>
                {
                    basicDeliverEventArgs = (ConsumeContext)i[0];
                    raisedException = (Exception)i[1];
                    return new ValueTask<AckStrategyAsync>(AckStrategies.AckAsync);
                }
            );

        mockBuilder = new MockBuilder(x => x
            .AddSingleton<IConventions>(conventions)
            .AddSingleton(consumeErrorStrategy)
        );
    }

    public async Task InitializeAsync()
    {
#pragma warning disable IDISP004
        await mockBuilder.PubSub.SubscribeAsync<MyMessage>(subscriptionId, _ => throw originalException);
#pragma warning restore IDISP004

        const string text = "Hello there, I am the text!";
        originalMessage = new MyMessage { Text = text };

        using var serializedMessage = new ReflectionBasedNewtonsoftJsonSerializer().MessageToBytes(typeof(MyMessage), originalMessage);

        // deliver a message
        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            consumerTag,
            deliveryTag,
            false, // redelivered
            typeName,
            "#",
            new BasicProperties
            {
                Type = typeName,
                CorrelationId = correlationId
            },
            serializedMessage.Memory
        );
    }
    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_ack()
    {
        await mockBuilder.Channels[2].Received().BasicAckAsync(deliveryTag, false);
    }

    [Fact]
    public void Should_invoke_the_consumer_error_strategy()
    {
        consumeErrorStrategy.Received()
            .HandleErrorAsync(Arg.Any<ConsumeContext>(), Arg.Any<Exception>());
    }

    [Fact]
    public void Should_pass_the_exception_to_consumerErrorStrategy()
    {
        raisedException.Should().BeSameAs(originalException);
    }

    [Fact]
    public void Should_pass_the_deliver_args_to_the_consumerErrorStrategy()
    {
        basicDeliverEventArgs.Should().NotBeNull();
        basicDeliverEventArgs.ReceivedInfo.ConsumerTag.Should().Be(consumerTag);
        basicDeliverEventArgs.ReceivedInfo.DeliveryTag.Should().Be(deliveryTag);
        basicDeliverEventArgs.ReceivedInfo.RoutingKey.Should().Be("#");
    }
}

public class When_a_subscription_is_cancelled_by_the_user : IAsyncLifetime
{
    private const string subscriptionId = "the_subscription_id";
    private const string consumerTag = "the_consumer_tag";
    private readonly MockBuilder mockBuilder;

    public When_a_subscription_is_cancelled_by_the_user()
    {
        var conventions = new Conventions(new DefaultTypeNameSerializer())
        {
            ConsumerTagConvention = () => consumerTag
        };

        mockBuilder = new MockBuilder(x => x.AddSingleton<IConventions>(conventions));

    }

    public async Task InitializeAsync()
    {
        await using var subscriptionResult = await mockBuilder.PubSub.SubscribeAsync<MyMessage>(subscriptionId, _ => { });
        using var are = new AutoResetEvent(false);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((ConsumerChannelDisposedEvent _) => Task.FromResult(are.Set()));
#pragma warning restore IDISP004
        are.WaitOne(500);
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_dispose_the_model()
    {
        mockBuilder.Consumers[0].Channel.Received().DisposeAsync();
    }
}
