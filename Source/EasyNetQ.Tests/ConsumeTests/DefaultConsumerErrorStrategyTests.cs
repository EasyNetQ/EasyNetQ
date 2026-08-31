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

    private static DefaultConsumeErrorStrategy CreateConsumerErrorStrategy(
        IConsumerConnection connectionMock,
        bool configurePublisherConfirm = false
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
            Substitute.For<ILogger<DefaultConsumeErrorStrategy>>(),
            channelDispatcher,
            Substitute.For<IMessageSerializer>(),
            new MessageTypeRegistry(new DefaultTypeNameSerializer()),
            Substitute.For<IConventions>(),
            Substitute.For<IErrorMessageSerializer>(),
            new ConnectionConfiguration { PublisherConfirms = configurePublisherConfirm }
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
