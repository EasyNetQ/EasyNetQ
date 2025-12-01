using System.Text;
using EasyNetQ.Consumer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.ConsumeTests;

public class DefaultConsumerErrorStrategyTests
{
    [Fact]
    public async Task Should_enable_publisher_confirm_when_configured_and_return_ack_when_confirm_received()
    {
        using var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var channelMock = Substitute.For<IChannel>();
#pragma warning disable IDISP004
        persistedConnectionMock.CreateChannelAsync(Arg.Is<CreateChannelOptions>(it => it.PublisherConfirmationTrackingEnabled && it.PublisherConfirmationsEnabled), default).Returns(channelMock);
#pragma warning restore IDISP004
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock, true);

        var ackStrategy = await consumerErrorStrategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!")
        );

        Assert.Equal(AckStrategies.AckAsync, ackStrategy);
    }

    [Fact]
    public async Task
        Should_enable_publisher_confirm_when_configured_and_return_nack_with_requeue_when_no_confirm_received()
    {
        using var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var channelMock = Substitute.For<IChannel>();
        channelMock.BasicPublishAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<RabbitMQ.Client.BasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromException(new PublishException(42, default)));

#pragma warning disable IDISP004
        persistedConnectionMock.CreateChannelAsync(Arg.Is<CreateChannelOptions>(it => it.PublisherConfirmationTrackingEnabled && it.PublisherConfirmationsEnabled), default).Returns(channelMock);
#pragma warning restore IDISP004
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock, true);

        var ackStrategy = await consumerErrorStrategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!")
        );

        Assert.Equal(AckStrategies.NackWithRequeueAsync, ackStrategy);
    }

    [Fact]
    public async Task Should_not_enable_publisher_confirm_when_not_configured_and_return_ack_when_no_confirm_received()
    {
        using var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IChannel>();
#pragma warning disable IDISP004
        persistedConnectionMock.CreateChannelAsync(new CreateChannelOptions(false, false), default)
            .Returns(modelMock);
#pragma warning restore IDISP004
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock);

        var ackStrategy = await consumerErrorStrategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"));

        Assert.Equal(AckStrategies.AckAsync, ackStrategy);
    }

    private static DefaultConsumeErrorStrategy CreateConsumerErrorStrategy(
        IConsumerConnection connectionMock, bool configurePublisherConfirm = false
    )
    {
        var consumerErrorStrategy = new DefaultConsumeErrorStrategy(
            Substitute.For<ILogger<DefaultConsumeErrorStrategy>>(),
            connectionMock,
            Substitute.For<ISerializer>(),
            Substitute.For<IConventions>(),
            Substitute.For<ITypeNameSerializer>(),
            Substitute.For<IErrorMessageSerializer>(),
            new ConnectionConfiguration { PublisherConfirms = configurePublisherConfirm }
        );
        return consumerErrorStrategy;
    }

    private static ConsumeContext CreateConsumerExecutionContext(byte[] originalMessageBody)
    {
        return new ConsumeContext(
            new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = "123",
                AppId = "456"
            },
            originalMessageBody,
            Substitute.For<IServiceProvider>(),
            CancellationToken.None
        );
    }

    private static byte[] CreateOriginalMessage()
    {
        const string originalMessage = "{ Text:\"Hello World\"}";
        var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);
        return originalMessageBody;
    }
}
