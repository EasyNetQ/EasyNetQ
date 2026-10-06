using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Producer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace EasyNetQ.Tests.ConsumeTests;

public class DefaultConsumerErrorStrategyTests
{
    [Fact]
    public async Task Should_ack_failed_message_after_confirmed_error_publish_when_publisher_confirms_on()
    {
        using var connection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IChannel>();
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(channel);
#pragma warning restore IDISP004
        var strategy = CreateConsumerErrorStrategy(connection, configurePublisherConfirm: true);

        var ackDecision = await strategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), TestContext.Current.CancellationToken
        );

        Assert.Equal(AckDecision.Ack, ackDecision);
        await channel.Received().BasicPublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_nack_with_requeue_when_error_publish_confirmation_fails()
    {
        using var connection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IChannel>();
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(channel);
#pragma warning restore IDISP004
        // client-side confirmation tracking faults BasicPublishAsync when the broker nacks
        channel.BasicPublishAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<RabbitMQ.Client.BasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()
            )
            .Returns(ValueTask.FromException(new PublishException(1, false)));
        var strategy = CreateConsumerErrorStrategy(connection, configurePublisherConfirm: true);

        var ackDecision = await strategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), TestContext.Current.CancellationToken
        );

        Assert.Equal(AckDecision.NackRequeue, ackDecision);
    }

    [Fact]
    public async Task Should_ack_failed_message_when_publisher_confirms_off()
    {
        using var connection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IChannel>();
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(channel);
#pragma warning restore IDISP004
        var strategy = CreateConsumerErrorStrategy(connection);

        var ackDecision = await strategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), TestContext.Current.CancellationToken
        );

        Assert.Equal(AckDecision.Ack, ackDecision);
        await channel.Received().BasicPublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_create_the_error_channel_with_confirms_and_client_side_tracking()
    {
        using var connection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IChannel>();
        CreateChannelOptions? createChannelOptions = null;
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Do<CreateChannelOptions>(x => createChannelOptions = x), Arg.Any<CancellationToken>())
            .Returns(channel);
#pragma warning restore IDISP004
        var strategy = CreateConsumerErrorStrategy(connection, configurePublisherConfirm: true);

        await strategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), TestContext.Current.CancellationToken
        );

        createChannelOptions.Should().NotBeNull();
        createChannelOptions!.PublisherConfirmationsEnabled.Should().BeTrue();
        createChannelOptions.PublisherConfirmationTrackingEnabled.Should().BeTrue();
        createChannelOptions.OutstandingPublisherConfirmationsRateLimiter.Should().NotBeNull();
    }

    private sealed class RecordingLogger : ILogger<DefaultConsumeErrorStrategy>
    {
        public List<EventId> Events { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Events.Add(eventId);
    }

    [Fact]
    public async Task Should_not_log_the_failed_body_unless_asked()
    {
        using var connection = Substitute.For<IConsumerConnection>();
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<IChannel>());
#pragma warning restore IDISP004
        var silent = new RecordingLogger();
        var verbose = new RecordingLogger();

        await CreateConsumerErrorStrategy(connection, logger: silent).HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("boom"), TestContext.Current.CancellationToken
        );
        await CreateConsumerErrorStrategy(connection, logger: verbose, options: new ConsumeErrorOptions { LogMessageBody = true }).HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("boom"), TestContext.Current.CancellationToken
        );

        silent.Events.Select(e => e.Id).Should().Contain(600).And.NotContain(601);
        verbose.Events.Select(e => e.Id).Should().Contain(601);
    }

    [Fact]
    public async Task Should_declare_the_error_queue_with_the_configured_arguments()
    {
        using var connection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IChannel>();
        IDictionary<string, object?>? declaredArguments = null;
        await channel.QueueDeclareAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Do<IDictionary<string, object?>>(a => declaredArguments = a), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(channel);
#pragma warning restore IDISP004
        var options = new ConsumeErrorOptions { ErrorQueueArguments = new Dictionary<string, object> { [Argument.QueueType] = QueueType.Quorum } };

        await CreateConsumerErrorStrategy(connection, options: options).HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("boom"), TestContext.Current.CancellationToken
        );

        declaredArguments.Should().NotBeNull();
        declaredArguments![Argument.QueueType].Should().Be(QueueType.Quorum);
    }

    [Fact]
    public async Task Should_declare_and_publish_to_the_configured_error_queue_and_exchange_names()
    {
        using var connection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IChannel>();
        string? declaredQueue = null, declaredExchange = null, boundQueue = null, boundExchange = null;
#pragma warning disable CS4014
        channel.QueueDeclareAsync(Arg.Do<string>(q => declaredQueue = q), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        channel.ExchangeDeclareAsync(Arg.Do<string>(e => declaredExchange = e), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        channel.QueueBindAsync(Arg.Do<string>(q => boundQueue = q), Arg.Do<string>(e => boundExchange = e), Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
#pragma warning restore CS4014
#pragma warning disable IDISP004
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(channel);
#pragma warning restore IDISP004
        var options = new ConsumeErrorOptions { QueueName = "app.errors", ExchangeName = "app.errors" };

        await CreateConsumerErrorStrategy(connection, options: options).HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("boom"), TestContext.Current.CancellationToken
        );

        declaredQueue.Should().Be("app.errors");
        declaredExchange.Should().Be("app.errors");
        boundQueue.Should().Be("app.errors");
        boundExchange.Should().Be("app.errors");
    }

    private static DefaultConsumeErrorStrategy CreateConsumerErrorStrategy(
        IConsumerConnection connectionMock,
        bool configurePublisherConfirm = false,
        ILogger<DefaultConsumeErrorStrategy>? logger = null,
        ConsumeErrorOptions? options = null
    )
    {
#pragma warning disable IDISP004
        var channelDispatcher = new SinglePersistentChannelDispatcher(
            Substitute.For<IProducerConnection>(),
            connectionMock,
            new PersistentChannelFactory(Substitute.For<ILogger<PersistentChannel>>(), Substitute.For<IEventBus>())
        );
#pragma warning restore IDISP004
        return new DefaultConsumeErrorStrategy(
            logger ?? Substitute.For<ILogger<DefaultConsumeErrorStrategy>>(),
            channelDispatcher,
            Substitute.For<IMessageSerializer>(),
            new MessageTypeRegistry(new DefaultTypeNameSerializer()),
            Substitute.For<IConventions>(),
            Substitute.For<IErrorMessageSerializer>(),
            new ConnectionConfiguration { PublisherConfirms = configurePublisherConfirm },
            options ?? new ConsumeErrorOptions()
        );
    }

    private static ConsumeContext CreateConsumerExecutionContext(byte[] originalMessageBody)
    {
        return TestContexts.Consume(
            new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = "123",
                AppId = "456"
            },
            originalMessageBody,
            Substitute.For<IServiceProvider>()
        );
    }

    private static byte[] CreateOriginalMessage()
    {
        const string originalMessage = "{ Text:\"Hello World\"}";
        return Encoding.UTF8.GetBytes(originalMessage);
    }
}
